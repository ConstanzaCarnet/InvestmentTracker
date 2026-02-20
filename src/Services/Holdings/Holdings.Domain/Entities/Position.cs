namespace Holdings.Domain.Entities;

public class Position
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePurchasePrice { get; set; }
    public DateTime LastTransactionDate { get; set; }

    public long LastProcessedSequenceNumber { get; set; } = 0;
}