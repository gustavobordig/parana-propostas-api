using Propostas.Domain.Common;

namespace Propostas.Domain.Score;

public static class CalculadoraScore
{
    public const int ScoreMinimo = 0;
    public const int ScoreMaximo = 1000;
    private const decimal Divisor = 10m;

    public static int Calcular(decimal rendaMensal)
    {
        if (rendaMensal < 0)
            throw new DomainException("Renda mensal não pode ser negativa.");

        var score = decimal.Floor(rendaMensal / Divisor);
        return (int)Math.Min(score, ScoreMaximo);
    }
}
