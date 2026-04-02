namespace EventBus.Messages.Events;

public record TransactionUpdatedEvent : TransactionBaseEvent
{
    public string PreviousTicker { get; init; } = string.Empty;//en caso de que el cliente haya actualizado el ticker
    public decimal PreviousQuantity { get; init; }
    public decimal PreviousPrice { get; init; }
    public decimal PreviousExchangeRate { get; init; }
    public Guid PreviousInstrumentId { get; set; }
    public string PreviousCurrency { get; init; } = string.Empty;
    public TransactionType PreviousType { get; init; }
}