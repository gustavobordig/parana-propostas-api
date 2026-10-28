using Microsoft.EntityFrameworkCore;
using Propostas.Application.Abstractions;
using Propostas.Domain.Entities;

namespace Propostas.Infrastructure.Persistence;

internal sealed class PropostaRepository(PropostasDbContext contexto) : IPropostaRepository
{
    public Task<bool> ExisteParaClienteAsync(Guid clienteId, CancellationToken cancellationToken) =>
        contexto.Propostas.AnyAsync(p => p.ClienteId == clienteId, cancellationToken);

    public Task<Proposta?> ObterPorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken) =>
        contexto.Propostas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ClienteId == clienteId, cancellationToken);

    public void Adicionar(Proposta proposta) => contexto.Propostas.Add(proposta);
}
