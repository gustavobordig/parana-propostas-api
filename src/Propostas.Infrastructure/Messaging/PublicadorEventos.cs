using Propostas.Application.Abstractions;
using Propostas.Infrastructure.Observability;
using MassTransit;

namespace Propostas.Infrastructure.Messaging;

internal sealed class PublicadorEventos(IPublishEndpoint publishEndpoint, CorrelationContext correlacao) : IPublicadorEventos
{
    public Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken) where TEvento : class =>
        publishEndpoint.Publish(evento, contexto => contexto.CorrelationId = correlacao.Id, cancellationToken);
}
