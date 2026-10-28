using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ParanaBanco.Credito.Contracts;
using Propostas.Application.Abstractions;
using Propostas.Application.Propostas.Gerar;
using Propostas.Domain.Entities;

namespace Propostas.Tests.Unit.Application;

public sealed class GerarPropostaHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 28, 17, 0, 0, TimeSpan.Zero);

    private readonly IPropostaRepository _repositorio = Substitute.For<IPropostaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPublicadorEventos _publicador = Substitute.For<IPublicadorEventos>();
    private readonly GerarPropostaHandler _handler;

    public GerarPropostaHandlerTests()
    {
        _handler = new GerarPropostaHandler(
            _repositorio,
            _unitOfWork,
            _publicador,
            new FakeTimeProvider(Agora),
            NullLogger<GerarPropostaHandler>.Instance);
    }

    private static ClienteCadastrado Evento(decimal renda) => new(Guid.NewGuid(), "Maria", renda, Agora);

    [Fact]
    public async Task HandleAsync_ClienteNovo_SalvaPropostaEPublicaEvento()
    {
        var evento = Evento(5_000m);

        await _handler.HandleAsync(evento, CancellationToken.None);

        _repositorio.Received(1).Adicionar(Arg.Is<Proposta>(p => p.ClienteId == evento.ClienteId && p.Score == 500));
        await _publicador.Received(1).PublicarAsync(
            Arg.Is<PropostaGerada>(e =>
                e.ClienteId == evento.ClienteId &&
                e.Score == 500 &&
                e.Status == "Aprovada" &&
                e.QuantidadeCartoes == 1 &&
                e.LimitePorCartao == 1_000m),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PropostaNegada_TambemPublicaEventoComStatusNegada()
    {
        var evento = Evento(500m);

        await _handler.HandleAsync(evento, CancellationToken.None);

        await _publicador.Received(1).PublicarAsync(
            Arg.Is<PropostaGerada>(e => e.Status == "Negada" && e.QuantidadeCartoes == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PropostaJaExistente_IgnoraEventoSemPublicarNemSalvar()
    {
        var evento = Evento(5_000m);
        _repositorio.ExisteParaClienteAsync(evento.ClienteId, Arg.Any<CancellationToken>()).Returns(true);

        await _handler.HandleAsync(evento, CancellationToken.None);

        _repositorio.DidNotReceive().Adicionar(Arg.Any<Proposta>());
        await _publicador.DidNotReceive().PublicarAsync(Arg.Any<PropostaGerada>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FalhaAoSalvar_PropagaExcecaoParaORetry()
    {
        _unitOfWork.SalvarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new TimeoutException()));

        await Assert.ThrowsAsync<TimeoutException>(() => _handler.HandleAsync(Evento(5_000m), CancellationToken.None));
    }
}
