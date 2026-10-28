using Microsoft.Extensions.DependencyInjection;
using Propostas.Application.Propostas.Gerar;
using Propostas.Application.Propostas.Obter;

namespace Propostas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<GerarPropostaHandler>();
        services.AddScoped<ObterPropostaHandler>();
        return services;
    }
}
