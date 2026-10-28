namespace Propostas.Infrastructure.Observability;

public sealed class CorrelationContext
{
    public const string Header = "X-Correlation-Id";

    public Guid Id { get; set; } = Guid.NewGuid();
}
