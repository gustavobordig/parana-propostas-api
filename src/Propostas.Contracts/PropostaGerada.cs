namespace ParanaBanco.Credito.Contracts;

public sealed record PropostaGerada(
    Guid PropostaId,
    Guid ClienteId,
    int Score,
    string Status,
    decimal LimitePorCartao,
    int QuantidadeCartoes,
    DateTimeOffset OcorridoEm)
{
    public int Versao { get; init; } = 1;
}
