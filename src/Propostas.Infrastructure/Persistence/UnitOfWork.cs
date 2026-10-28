using Propostas.Application.Abstractions;

namespace Propostas.Infrastructure.Persistence;

internal sealed class UnitOfWork(PropostasDbContext contexto) : IUnitOfWork
{
    public Task SalvarAsync(CancellationToken cancellationToken) => contexto.SaveChangesAsync(cancellationToken);
}
