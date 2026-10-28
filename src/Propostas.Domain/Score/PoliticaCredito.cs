using Propostas.Domain.Common;
using Propostas.Domain.Entities;

namespace Propostas.Domain.Score;

public sealed record DecisaoCredito(StatusProposta Status, decimal LimitePorCartao, int QuantidadeCartoes);

public static class PoliticaCredito
{
    private static readonly DecisaoCredito Negado = new(StatusProposta.Negada, 0m, 0);
    private static readonly DecisaoCredito AprovadoBasico = new(StatusProposta.Aprovada, 1_000m, 1);
    private static readonly DecisaoCredito AprovadoPremium = new(StatusProposta.Aprovada, 5_000m, 2);

    public static DecisaoCredito Avaliar(int score) => score switch
    {
        < CalculadoraScore.ScoreMinimo or > CalculadoraScore.ScoreMaximo =>
            throw new DomainException($"Score {score} fora do intervalo {CalculadoraScore.ScoreMinimo}–{CalculadoraScore.ScoreMaximo}."),
        <= 100 => Negado,
        <= 500 => AprovadoBasico,
        _ => AprovadoPremium
    };
}
