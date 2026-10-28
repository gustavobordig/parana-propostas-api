using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ParanaBanco.Credito.Contracts;
using Propostas.Application.Abstractions;
using Propostas.Application.Propostas.Gerar;
using Propostas.Domain.Entities;
using Propostas.Infrastructure.Messaging;
using Propostas.Infrastructure.Messaging.Consumers;
using Propostas.Infrastructure.Observability;

namespace Propostas.Tests.Unit.Messaging;

public sealed class ClienteCadastradoConsumerTests : IAsyncLifetime
{
    private readonly IPropostaRepository _repositorio = Substitute.For<IPropostaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_repositorio)
            .AddSingleton(_unitOfWork)
            .AddSingleton<TimeProvider>(new FakeTimeProvider(DateTimeOffset.UtcNow))
            .AddScoped<CorrelationContext>()
            .AddScoped<IPublicadorEventos, PublicadorEventos>()
            .AddScoped<GerarPropostaHandler>()
            .AddMassTransitTestHarness(bus => bus.AddConsumer<ClienteCadastradoConsumer>(consumer =>
                consumer.UseMessageRetry(retry => ConfiguracaoResiliencia.AplicarRetry(retry, TimeSpan.FromMilliseconds(10)))))
            .BuildServiceProvider(validateScopes: true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Consume_ClienteComRendaAlta_PublicaPropostaAprovadaComDoisCartoes()
    {
        var evento = new ClienteCadastrado(Guid.NewGuid(), "Maria", 10_000m, DateTimeOffset.UtcNow);

        await _harness.Bus.Publish(evento);

        Assert.True(await _harness.Consumed.Any<ClienteCadastrado>());
        Assert.True(await _harness.Published.Any<PropostaGerada>(m =>
            m.Context.Message.ClienteId == evento.ClienteId &&
            m.Context.Message.Status == "Aprovada" &&
            m.Context.Message.QuantidadeCartoes == 2));
    }

    [Fact]
    public async Task Consume_EventoComCorrelationId_PropagaCorrelationIdParaPropostaGerada()
    {
        var correlationId = Guid.NewGuid();
        var evento = new ClienteCadastrado(Guid.NewGuid(), "Maria", 3_000m, DateTimeOffset.UtcNow);

        await _harness.Bus.Publish(evento, contexto => contexto.CorrelationId = correlationId);

        Assert.True(await _harness.Published.Any<PropostaGerada>(m => m.Context.CorrelationId == correlationId));
    }

    [Fact]
    public async Task Consume_FalhaTransitoriaQueSeResolve_ProcessaAposRetry()
    {
        var falhas = 0;
        _unitOfWork.SalvarAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            ++falhas <= 2 ? Task.FromException(new TimeoutException("SQL Server indisponível")) : Task.CompletedTask);

        await _harness.Bus.Publish(new ClienteCadastrado(Guid.NewGuid(), "Maria", 5_000m, DateTimeOffset.UtcNow));

        Assert.True(await _harness.Consumed.Any<ClienteCadastrado>());
        Assert.False(await _harness.Published.Any<Fault<ClienteCadastrado>>());
        await _unitOfWork.Received(3).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_FalhaPersistente_EsgotaRetryEGeraFault()
    {
        _unitOfWork.SalvarAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException("SQL Server indisponível"));

        await _harness.Bus.Publish(new ClienteCadastrado(Guid.NewGuid(), "Maria", 5_000m, DateTimeOffset.UtcNow));

        Assert.True(await _harness.Published.Any<Fault<ClienteCadastrado>>());
        await _unitOfWork.Received(1 + ConfiguracaoResiliencia.Tentativas).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_RendaNegativa_NaoRepeteEGeraFaultImediato()
    {
        await _harness.Bus.Publish(new ClienteCadastrado(Guid.NewGuid(), "Maria", -1m, DateTimeOffset.UtcNow));

        Assert.True(await _harness.Published.Any<Fault<ClienteCadastrado>>());
        await _repositorio.Received(1).ExisteParaClienteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _repositorio.DidNotReceive().Adicionar(Arg.Any<Proposta>());
    }
}
