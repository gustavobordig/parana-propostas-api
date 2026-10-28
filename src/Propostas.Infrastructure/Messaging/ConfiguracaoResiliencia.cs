using MassTransit;
using Propostas.Domain.Common;

namespace Propostas.Infrastructure.Messaging;

internal static class ConfiguracaoResiliencia
{
    public const int Tentativas = 3;

    public static void AplicarRetry(IRetryConfigurator retry, TimeSpan? intervaloMinimo = null)
    {
        var minimo = intervaloMinimo ?? TimeSpan.FromSeconds(1);

        retry.Exponential(Tentativas, minimo, minimo * 10, minimo);
        retry.Ignore<DomainException>();
    }

    public static void AplicarKillSwitch(IReceiveEndpointConfigurator endpoint) =>
        endpoint.UseKillSwitch(options => options
            .SetActivationThreshold(10)
            .SetTripThreshold(0.15)
            .SetRestartTimeout(m: 1));
}
