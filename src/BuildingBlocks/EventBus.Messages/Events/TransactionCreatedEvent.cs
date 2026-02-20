namespace EventBus.Messages.Events;

//evitamos repetir codigo, do not repeat yourself (DRY)
public record TransactionCreatedEvent : TransactionBaseEvent
{ }

/*public record TransactionCreatedEvent
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Ticker { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal Price { get; init; }
    public string Type { get; init; } = string.Empty;
    //agregamos la fecha para que no se procesen mensajes viejos
    public DateTime CreatedAt { get; init; }
}*/