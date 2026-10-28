using Propostas.Domain.Common;
using Propostas.Domain.Score;

namespace Propostas.Tests.Unit.Domain;

public sealed class CalculadoraScoreTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(9.99, 0)]
    [InlineData(1_000, 100)]
    [InlineData(1_010, 101)]
    [InlineData(5_000, 500)]
    [InlineData(5_009.99, 500)]
    [InlineData(5_010, 501)]
    [InlineData(10_000, 1000)]
    [InlineData(250_000, 1000)]
    public void Calcular_RendaMensal_RetornaRendaDivididaPorDezLimitadaAMil(decimal renda, int scoreEsperado)
    {
        var score = CalculadoraScore.Calcular(renda);

        Assert.Equal(scoreEsperado, score);
    }

    [Fact]
    public void Calcular_RendaNegativa_LancaDomainException()
    {
        Assert.Throws<DomainException>(() => CalculadoraScore.Calcular(-1m));
    }
}
