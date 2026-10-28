using Propostas.Domain.Common;
using Propostas.Domain.Entities;
using Propostas.Domain.Score;

namespace Propostas.Tests.Unit.Domain;

public sealed class PoliticaCreditoTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Avaliar_ScoreAteCem_NegaCredito(int score)
    {
        var decisao = PoliticaCredito.Avaliar(score);

        Assert.Equal(StatusProposta.Negada, decisao.Status);
        Assert.Equal(0, decisao.QuantidadeCartoes);
        Assert.Equal(0m, decisao.LimitePorCartao);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(300)]
    [InlineData(500)]
    public void Avaliar_ScoreEntre101E500_AprovaUmCartaoDeMilReais(int score)
    {
        var decisao = PoliticaCredito.Avaliar(score);

        Assert.Equal(StatusProposta.Aprovada, decisao.Status);
        Assert.Equal(1, decisao.QuantidadeCartoes);
        Assert.Equal(1_000m, decisao.LimitePorCartao);
    }

    [Theory]
    [InlineData(501)]
    [InlineData(750)]
    [InlineData(1000)]
    public void Avaliar_ScoreEntre501E1000_AprovaDoisCartoesDeCincoMilReais(int score)
    {
        var decisao = PoliticaCredito.Avaliar(score);

        Assert.Equal(StatusProposta.Aprovada, decisao.Status);
        Assert.Equal(2, decisao.QuantidadeCartoes);
        Assert.Equal(5_000m, decisao.LimitePorCartao);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Avaliar_ScoreForaDoIntervalo_LancaDomainException(int score)
    {
        Assert.Throws<DomainException>(() => PoliticaCredito.Avaliar(score));
    }
}
