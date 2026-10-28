using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParanaBanco.Credito.Contracts;
using Propostas.Application.Abstractions;
using Propostas.Infrastructure.Messaging;
using Propostas.Infrastructure.Messaging.Consumers;
using Propostas.Infrastructure.Observability;
using Propostas.Infrastructure.Persistence;

namespace Propostas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuracao)
    {
        var sqlServer = configuracao.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer não configurada.");

        var rabbitMq = RabbitMqOptions.DaUrl(configuracao["RabbitMq:Url"]
            ?? throw new InvalidOperationException("RabbitMq:Url não configurada."));

        services.AddDbContext<PropostasDbContext>(options =>
            options.UseSqlServer(sqlServer, sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IPropostaRepository, PropostaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPublicadorEventos, PublicadorEventos>();
        services.AddScoped<CorrelationContext>();

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<PropostasDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
            });

            bus.AddConsumer<ClienteCadastradoConsumer, ClienteCadastradoConsumerDefinition>();

            bus.UsingRabbitMq((contexto, rabbit) =>
            {
                rabbit.Host(rabbitMq.Host, rabbitMq.Porta, rabbitMq.VirtualHost, host =>
                {
                    host.Username(rabbitMq.Usuario);
                    host.Password(rabbitMq.Senha);
                });

                rabbit.Message<ClienteCadastrado>(m => m.SetEntityName("cliente-cadastrado"));
                rabbit.Message<PropostaGerada>(m => m.SetEntityName("proposta-gerada"));

                rabbit.ConfigureEndpoints(contexto);
            });
        });

        services.AddHealthChecks().AddDbContextCheck<PropostasDbContext>("sqlserver");

        return services;
    }
}
