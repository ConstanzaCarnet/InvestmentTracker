namespace EventBus.Messages.Common;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    // Momento real en que ocurrió
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;

    // VERSION DEL CONTRATO DEL EVENTO (NO del negocio)
    public int EventVersion { get; init; } = 1;
}
