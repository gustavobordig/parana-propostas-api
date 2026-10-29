# Propostas API

Microsserviço de **proposta de crédito** do fluxo de concessão de crédito. Consome o evento `ClienteCadastrado`, calcula o score do cliente, aplica a política de crédito e publica o evento `PropostaGerada`, que dispara a emissão dos cartões.

| | |
|---|---|
| **Swagger (produção)** | https://parana-propostas-api-production.up.railway.app/swagger |
| **Front da solução** | https://parana-credito-web.vercel.app |
| **Stack** | .NET 10 · ASP.NET Core · EF Core + SQL Server · MassTransit 8 + RabbitMQ · xUnit |

## Sumário

- [A solução completa](#a-solução-completa)
- [Regra de score](#regra-de-score)
- [Fluxo de mensagens](#fluxo-de-mensagens)
  - [Evento consumido: ClienteCadastrado](#evento-consumido-clientecadastrado)
  - [Evento publicado: PropostaGerada](#evento-publicado-propostagerada)
- [Resiliência](#resiliência)
- [Endpoints](#endpoints)
- [Arquitetura](#arquitetura)
- [Decisões técnicas](#decisões-técnicas)
- [Como executar localmente](#como-executar-localmente)
  - [Variáveis de ambiente](#variáveis-de-ambiente)
- [Testes](#testes)
- [Deploy](#deploy)
- [Próximos passos](#próximos-passos)

## A solução completa

O fluxo é dividido em três microsserviços independentes, um por repositório, que se comunicam apenas por eventos:

| Serviço | Responsabilidade | Repositório |
|---|---|---|
| **Clientes** | Cadastra o cliente e publica `ClienteCadastrado` | [parana-clientes-api](https://github.com/gustavobordig/parana-clientes-api) |
| **Propostas** | Calcula o score, decide o crédito e publica `PropostaGerada` | este |
| **Cartões** | Emite os cartões da proposta aprovada | [parana-cartoes-api](https://github.com/gustavobordig/parana-cartoes-api) |
| Front | Cadastro e acompanhamento do fluxo em tempo real | [parana-credito-web](https://github.com/gustavobordig/parana-credito-web) |

```mermaid
flowchart LR
    U[Front / cliente HTTP] -->|POST /api/v1/clientes| C[Clientes API]
    C -->|salva cliente + evento<br/>na mesma transação| DBC[(SQL Server<br/>clientes_db)]
    C -->|ClienteCadastrado<br/>via outbox| EX1{{exchange<br/>cliente-cadastrado}}
    EX1 --> Q1[fila<br/>proposta-cliente-cadastrado]
    Q1 --> P[Propostas API]
    Q1 -. falha após retry .-> DLQ1[proposta-cliente-cadastrado_error]
    P --> DBP[(SQL Server<br/>propostas_db)]
    P -->|PropostaGerada| EX2{{exchange<br/>proposta-gerada}}
    EX2 --> Q2[fila<br/>cartao-proposta-gerada]
    Q2 --> K[Cartões API]
    Q2 -. falha após retry .-> DLQ2[cartao-proposta-gerada_error]
    K --> DBK[(MongoDB<br/>cartoes_db)]
```

## Regra de score

O case define as faixas de crédito, mas não como o score é calculado. A regra adotada é simples, determinística e fácil de explicar:

```
score = min(1000, floor(rendaMensal / 10))
```

| Score | Status | Limite por cartão | Cartões | Exemplo de renda |
|---|---|---|---|---|
| 0 – 100 | Negada | — | 0 | até R$ 1.009,99 |
| 101 – 500 | Aprovada | R$ 1.000,00 | 1 | R$ 1.010 a R$ 5.009,99 |
| 501 – 1000 | Aprovada | R$ 5.000,00 | 2 | a partir de R$ 5.010 |

Na faixa mais alta, o limite de R$ 5.000 é individual: até R$ 10.000 no total.

## Fluxo de mensagens

1. O consumer `ClienteCadastradoConsumer` recebe o evento da fila `proposta-cliente-cadastrado`.
2. Se o cliente já tem proposta, o evento é descartado (idempotência).
3. Calcula o score, aplica a política, salva a proposta e publica `PropostaGerada`, tudo na mesma transação (outbox).

**Propostas negadas também são publicadas.** O serviço de Cartões decide não emitir; assim, todo cadastro tem um desfecho rastreável.

### Evento consumido: `ClienteCadastrado`

```json
{ "clienteId": "guid", "nome": "Maria Silva", "rendaMensal": 7500.00, "ocorridoEm": "2026-10-29T10:00:00Z", "versao": 1 }
```

### Evento publicado: `PropostaGerada`

```json
{
  "propostaId": "guid", "clienteId": "guid", "score": 750, "status": "Aprovada",
  "limitePorCartao": 5000.00, "quantidadeCartoes": 2, "ocorridoEm": "2026-10-29T10:00:01Z", "versao": 1
}
```

## Resiliência

| Mecanismo | Configuração |
|---|---|
| **Retry exponencial** | 3 novas tentativas, começando em 1 s, para falhas transitórias (banco fora, timeout). |
| **Sem retry para erro de regra** | `DomainException` (ex.: renda negativa) vai direto para a fila de erro: tentar de novo não resolveria. |
| **Dead-letter** | Esgotadas as tentativas, a mensagem vai para `proposta-cliente-cadastrado_error` e um `Fault<ClienteCadastrado>` é publicado. |
| **Circuit breaker** (kill switch) | Se 15% das últimas 10 mensagens falharem, o consumo pausa por 1 minuto em vez de insistir num banco indisponível. |
| **Inbox** | Deduplica mensagens pelo `MessageId`: uma reentrega do broker não é processada duas vezes. |
| **Outbox** | Proposta e `PropostaGerada` são gravados na mesma transação; o evento só sai depois do commit. |
| **Idempotência** | Checagem prévia + índice único em `ClienteId`: no máximo uma proposta por cliente, mesmo com consumo concorrente. |

Mensagens na fila `_error` podem ser reprocessadas pelo painel do RabbitMQ, movendo-as de volta para a fila principal depois de corrigida a causa.

## Endpoints

| Método | Rota | Respostas |
|---|---|---|
| `GET` | `/api/v1/propostas/{clienteId}` | `200` · `404` enquanto a proposta ainda não foi gerada |
| `GET` | `/health` | `Healthy` quando SQL Server e RabbitMQ estão acessíveis |

A proposta é gerada de forma assíncrona: logo após o cadastro, um `404` significa que o evento ainda está sendo processado.

## Arquitetura

```
src/
├── Propostas.Api             Controller, ProblemDetails, Swagger, versionamento, middlewares
├── Propostas.Application     Casos de uso (gerar, consultar) e portas
├── Propostas.Domain          Proposta, CalculadoraScore e PoliticaCredito, sem dependências externas
├── Propostas.Infrastructure  EF Core, migrations, MassTransit (consumer, retry, kill switch, inbox/outbox)
└── Propostas.Contracts       Records dos eventos de integração
tests/
└── Propostas.Tests.Unit      xUnit + NSubstitute + test harness do MassTransit
```

O consumer fica na Infrastructure e só traduz a mensagem para o caso de uso: a regra de negócio não depende do MassTransit.

## Decisões técnicas

| Decisão | Motivo |
|---|---|
| **Score pela renda** | O case não define a regra; renda ÷ 10 é determinística, testável e cobre as três faixas com valores realistas. |
| **Política como função pura** | `PoliticaCredito.Avaliar(score)` não tem estado: as fronteiras (100/101, 500/501) são testadas diretamente. |
| **MassTransit 8** | Retry, kill switch, dead-letter, inbox e outbox prontos; trocar RabbitMQ por Azure Service Bus é configuração. Fixado na v8 por licença (a v9 é comercial). |
| **Status como texto no evento** | O contrato fica legível no broker e não depende da numeração do enum. |
| **Correlation ID** | Vem do evento do Clientes e segue no `PropostaGerada`: o fluxo é rastreável nos logs dos três serviços. |
| **Limites e concorrência** | `PrefetchCount` 16 e até 8 mensagens em paralelo por instância. |

## Como executar localmente

**Pré-requisitos:** .NET 10 SDK e Docker.

RabbitMQ e SQL Server vêm do `docker-compose` do [parana-clientes-api](https://github.com/gustavobordig/parana-clientes-api), que cria a rede `parana-credito`. Suba aquele primeiro e depois este:

```bash
docker compose up -d --build
```

Swagger em http://localhost:5102/swagger. A migration cria o banco `propostas_db` na inicialização.

Para rodar fora do Docker, com a infraestrutura do Clientes no ar:

```bash
dotnet run --project src/Propostas.Api
```

### Variáveis de ambiente

| Variável | Descrição | Padrão local |
|---|---|---|
| `ConnectionStrings__SqlServer` | Conexão com o SQL Server | `Server=localhost,1433;Database=propostas_db;...` |
| `RabbitMq__Url` | URL AMQP do RabbitMQ | `amqp://guest:guest@localhost:5672/` |
| `Cors__Origens__0` | Origem liberada para o front | `http://localhost:5173` |
| `Database__AplicarMigrations` | Aplica as migrations na inicialização | `true` |
| `PORT` | Porta HTTP (usada pelo Railway) | `8080` no container |

## Testes

```bash
dotnet test
```

35 testes de unidade com xUnit, no padrão AAA e com nomes `Metodo_Cenario_ResultadoEsperado`:

- **CalculadoraScore:** cada faixa e o teto de 1000; renda negativa é rejeitada.
- **PoliticaCredito:** as fronteiras 0, 100, 101, 500, 501 e 1000; score fora do intervalo é rejeitado.
- **Geração de proposta:** salva e publica; proposta negada também publica; cliente já processado é ignorado; falha ao salvar é propagada para o retry.
- **Consumer** (test harness do MassTransit, sem RabbitMQ real):
  - publica proposta aprovada com dois cartões;
  - propaga o Correlation ID para o `PropostaGerada`;
  - falha transitória é resolvida pelo retry;
  - falha persistente gera `Fault` depois de 1 + 3 tentativas;
  - erro de regra gera `Fault` sem retry.

O CI (GitHub Actions) roda build e testes a cada push e pull request.

## Deploy

As três APIs rodam no **Railway**, cada uma a partir do próprio `Dockerfile`, junto com SQL Server, MongoDB e RabbitMQ no mesmo projeto. O front está na Vercel.

![Projeto no Railway: as três APIs, SQL Server, MongoDB e RabbitMQ](docs/deploy-railway.webp)

As setas mostram as dependências de cada serviço: Clientes e Propostas usam o SQL Server (`server`), Cartões usa o MongoDB, e as três se conectam ao RabbitMQ.

## Próximos passos

- Testes de integração com Testcontainers (SQL Server e RabbitMQ reais).
- Redelivery atrasado (segundo nível de retry) para falhas mais longas.
- Provisionar a topologia do RabbitMQ no deploy, para não depender da ordem em que os serviços sobem.
- Rastreamento distribuído com OpenTelemetry.
