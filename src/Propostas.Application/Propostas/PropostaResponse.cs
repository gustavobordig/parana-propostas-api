using Propostas.Domain.Entities;

namespace Propostas.Application.Propostas;

public sealed record PropostaResponse(
    Guid Id,
    Guid ClienteId,
    int Score,
    string Status,
    decimal LimitePorCartao,
    int QuantidadeCartoes,
    decimal LimiteTotal,
    DateTimeOffset CriadaEm)
{
    public static PropostaResponse De(Proposta proposta) => new(
        proposta.Id,
        proposta.ClienteId,
        proposta.Score,
        proposta.Status.ToString(),
        proposta.LimitePorCartao,
        proposta.QuantidadeCartoes,
        proposta.LimiteTotal,
        proposta.CriadaEm);
}
