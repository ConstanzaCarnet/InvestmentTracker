namespace Transactions.Infrastructure.Outbox;
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Type { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedOnUtc { get; set; }

    public Guid IdempotencyKey { get; set; }

    public string? Error { get; set; }
}
