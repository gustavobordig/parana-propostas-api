using Propostas.Domain.Entities;

namespace Propostas.Application.Abstractions;

public interface IPropostaRepository
{
    Task<bool> ExisteParaClienteAsync(Guid clienteId, CancellationToken cancellationToken);

    Task<Proposta?> ObterPorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken);

    void Adicionar(Proposta proposta);
}
