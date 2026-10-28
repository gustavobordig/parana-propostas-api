using Propostas.Application.Abstractions;

namespace Propostas.Application.Propostas.Obter;

public sealed class ObterPropostaHandler(IPropostaRepository repositorio)
{
    public async Task<PropostaResponse?> HandleAsync(Guid clienteId, CancellationToken cancellationToken)
    {
        var proposta = await repositorio.ObterPorClienteIdAsync(clienteId, cancellationToken);
        return proposta is null ? null : PropostaResponse.De(proposta);
    }
}
