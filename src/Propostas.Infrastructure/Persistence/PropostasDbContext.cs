using MassTransit;
using Microsoft.EntityFrameworkCore;
using Propostas.Domain.Entities;

namespace Propostas.Infrastructure.Persistence;

public sealed class PropostasDbContext(DbContextOptions<PropostasDbContext> options) : DbContext(options)
{
    public DbSet<Proposta> Propostas => Set<Proposta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropostasDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
