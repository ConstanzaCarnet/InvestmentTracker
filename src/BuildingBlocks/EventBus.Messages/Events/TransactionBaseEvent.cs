namespace EventBus.Messages.Events;

public record TransactionBaseEvent
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Ticker { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal Price { get; init; }
    public TransactionType Type { get; init; }  // "Buy" o "Sell"
    public DateTime CreatedAt { get; init; }
    //control de lectura
    public long SequenceNumber { get; init; }
}