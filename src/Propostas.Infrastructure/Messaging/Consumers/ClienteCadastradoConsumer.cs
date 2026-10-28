using MassTransit;
using Microsoft.Extensions.Logging;
using ParanaBanco.Credito.Contracts;
using Propostas.Application.Propostas.Gerar;
using Propostas.Infrastructure.Observability;
using Propostas.Infrastructure.Persistence;

namespace Propostas.Infrastructure.Messaging.Consumers;

internal sealed class ClienteCadastradoConsumer(
    GerarPropostaHandler gerarProposta,
    CorrelationContext correlacao,
    ILogger<ClienteCadastradoConsumer> logger) : IConsumer<ClienteCadastrado>
{
    public async Task Consume(ConsumeContext<ClienteCadastrado> context)
    {
        correlacao.Id = context.CorrelationId ?? Guid.NewGuid();

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlacao.Id }))
        {
            await gerarProposta.HandleAsync(context.Message, context.CancellationToken);
        }
    }
}

internal sealed class ClienteCadastradoConsumerDefinition : ConsumerDefinition<ClienteCadastradoConsumer>
{
    public ClienteCadastradoConsumerDefinition()
    {
        EndpointName = "proposta-cliente-cadastrado";
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ClienteCadastradoConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 16;

        ConfiguracaoResiliencia.AplicarKillSwitch(endpointConfigurator);
        endpointConfigurator.UseMessageRetry(retry => ConfiguracaoResiliencia.AplicarRetry(retry));

        endpointConfigurator.UseEntityFrameworkOutbox<PropostasDbContext>(context);
    }
}
