using Microsoft.Extensions.Logging;
using ParanaBanco.Credito.Contracts;
using Propostas.Application.Abstractions;
using Propostas.Domain.Entities;

namespace Propostas.Application.Propostas.Gerar;

public sealed class GerarPropostaHandler(
    IPropostaRepository repositorio,
    IUnitOfWork unitOfWork,
    IPublicadorEventos publicador,
    TimeProvider relogio,
    ILogger<GerarPropostaHandler> logger)
{
    public async Task HandleAsync(ClienteCadastrado evento, CancellationToken cancellationToken)
    {
        if (await repositorio.ExisteParaClienteAsync(evento.ClienteId, cancellationToken))
        {
            logger.LogInformation("Proposta do cliente {ClienteId} já gerada; evento ignorado", evento.ClienteId);
            return;
        }

        var agora = relogio.GetUtcNow();
        var proposta = Proposta.Gerar(evento.ClienteId, evento.RendaMensal, agora);

        repositorio.Adicionar(proposta);

        await publicador.PublicarAsync(
            new PropostaGerada(
                proposta.Id,
                proposta.ClienteId,
                proposta.Score,
                proposta.Status.ToString(),
                proposta.LimitePorCartao,
                proposta.QuantidadeCartoes,
                agora),
            cancellationToken);

        await unitOfWork.SalvarAsync(cancellationToken);

        logger.LogInformation(
            "Proposta {PropostaId} do cliente {ClienteId}: score {Score}, {Status}, {QuantidadeCartoes} cartão(ões)",
            proposta.Id, proposta.ClienteId, proposta.Score, proposta.Status, proposta.QuantidadeCartoes);
    }
}
