namespace EventBus.Messages.Events;

public record TransactionBaseEvent
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid InstrumentId { get; set; }
    public string Ticker { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
    public decimal ConversionRatio { get; init; }
    public TransactionType Type { get; init; }  // "Buy" o "Sell"
    public DateTime CreatedAt { get; init; }
    //control de lectura de los mensajes, asi evitamos que se lean en cualquier orden, y que se procesen mensajes viejos o duplicados
    public long SequenceNumber { get; init; }
}