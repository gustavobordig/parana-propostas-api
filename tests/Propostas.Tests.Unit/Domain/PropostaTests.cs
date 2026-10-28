using Propostas.Domain.Entities;

namespace Propostas.Tests.Unit.Domain;

public sealed class PropostaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 28, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Gerar_RendaAlta_AprovaDoisCartoesComLimiteTotalDeDezMil()
    {
        var clienteId = Guid.NewGuid();

        var proposta = Proposta.Gerar(clienteId, 12_000m, Agora);

        Assert.Equal(clienteId, proposta.ClienteId);
        Assert.Equal(1000, proposta.Score);
        Assert.Equal(StatusProposta.Aprovada, proposta.Status);
        Assert.Equal(2, proposta.QuantidadeCartoes);
        Assert.Equal(5_000m, proposta.LimitePorCartao);
        Assert.Equal(10_000m, proposta.LimiteTotal);
        Assert.Equal(Agora, proposta.CriadaEm);
    }

    [Fact]
    public void Gerar_RendaMedia_AprovaUmCartao()
    {
        var proposta = Proposta.Gerar(Guid.NewGuid(), 3_000m, Agora);

        Assert.Equal(300, proposta.Score);
        Assert.Equal(1, proposta.QuantidadeCartoes);
        Assert.Equal(1_000m, proposta.LimiteTotal);
    }

    [Fact]
    public void Gerar_RendaBaixa_NegaSemLimite()
    {
        var proposta = Proposta.Gerar(Guid.NewGuid(), 800m, Agora);

        Assert.Equal(80, proposta.Score);
        Assert.Equal(StatusProposta.Negada, proposta.Status);
        Assert.Equal(0m, proposta.LimiteTotal);
    }
}
