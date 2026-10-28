using Propostas.Domain.Score;

namespace Propostas.Domain.Entities;

public sealed class Proposta
{
    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public decimal RendaMensal { get; private set; }
    public int Score { get; private set; }
    public StatusProposta Status { get; private set; }
    public decimal LimitePorCartao { get; private set; }
    public int QuantidadeCartoes { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }

    public decimal LimiteTotal => LimitePorCartao * QuantidadeCartoes;

    private Proposta()
    {
    }

    public static Proposta Gerar(Guid clienteId, decimal rendaMensal, DateTimeOffset agora)
    {
        var score = CalculadoraScore.Calcular(rendaMensal);
        var decisao = PoliticaCredito.Avaliar(score);

        return new Proposta
        {
            Id = Guid.CreateVersion7(agora),
            ClienteId = clienteId,
            RendaMensal = rendaMensal,
            Score = score,
            Status = decisao.Status,
            LimitePorCartao = decisao.LimitePorCartao,
            QuantidadeCartoes = decisao.QuantidadeCartoes,
            CriadaEm = agora
        };
    }
}
