using NSubstitute;
using Propostas.Application.Abstractions;
using Propostas.Application.Propostas.Obter;
using Propostas.Domain.Entities;

namespace Propostas.Tests.Unit.Application;

public sealed class ObterPropostaHandlerTests
{
    private readonly IPropostaRepository _repositorio = Substitute.For<IPropostaRepository>();
    private readonly ObterPropostaHandler _handler;

    public ObterPropostaHandlerTests()
    {
        _handler = new ObterPropostaHandler(_repositorio);
    }

    [Fact]
    public async Task HandleAsync_PropostaExistente_RetornaDadosComLimiteTotal()
    {
        var proposta = Proposta.Gerar(Guid.NewGuid(), 10_000m, DateTimeOffset.UtcNow);
        _repositorio.ObterPorClienteIdAsync(proposta.ClienteId, Arg.Any<CancellationToken>()).Returns(proposta);

        var resposta = await _handler.HandleAsync(proposta.ClienteId, CancellationToken.None);

        Assert.NotNull(resposta);
        Assert.Equal("Aprovada", resposta.Status);
        Assert.Equal(10_000m, resposta.LimiteTotal);
    }

    [Fact]
    public async Task HandleAsync_PropostaInexistente_RetornaNulo()
    {
        _repositorio.ObterPorClienteIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Proposta?)null);

        var resposta = await _handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(resposta);
    }
}
