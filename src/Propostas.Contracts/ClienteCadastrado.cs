namespace ParanaBanco.Credito.Contracts;

public sealed record ClienteCadastrado(
    Guid ClienteId,
    string Nome,
    decimal RendaMensal,
    DateTimeOffset OcorridoEm)
{
    public int Versao { get; init; } = 1;
}
