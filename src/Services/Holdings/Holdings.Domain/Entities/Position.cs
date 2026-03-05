namespace Holdings.Domain.Entities;
//"la posición de un inversor sobre un instrumento"
public class Position
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Ticker { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal TotalInvestedAmount { get; private set; }

    public decimal AveragePurchasePrice =>
        Quantity == 0 ? 0 : TotalInvestedAmount / Quantity;

    public long LastProcessedSequenceNumber { get; private set; }
    public DateTime LastTransactionDate { get; private set; }

    private Position() { }

    public Position(Guid userId, string ticker)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Ticker = ticker;
        Quantity = 0;
        TotalInvestedAmount = 0;
        LastProcessedSequenceNumber = 0;
    }

    public void Buy(decimal quantity, decimal price, long sequence, DateTime createdAt)
    {
        ValidateSequence(sequence);

        Quantity += quantity;
        TotalInvestedAmount += quantity * price;

        ApplyMetadata(sequence, createdAt);
    }

    public bool Sell(decimal quantity, long sequence, DateTime createdAt)
    {
        ValidateSequence(sequence);

        if (quantity > Quantity)
            throw new InvalidOperationException("Insufficient quantity");

        TotalInvestedAmount -= quantity * AveragePurchasePrice;
        Quantity -= quantity;

        ApplyMetadata(sequence, createdAt);

        return Quantity == 0;
    }

    private void ValidateSequence(long sequence)
    {
        if (sequence <= LastProcessedSequenceNumber)
            throw new InvalidOperationException("Duplicate or old event");

        if (sequence != LastProcessedSequenceNumber + 1)
            throw new InvalidOperationException("Out of order event");
    }

    private void ApplyMetadata(long sequence, DateTime createdAt)
    {
        LastProcessedSequenceNumber = sequence;
        LastTransactionDate = createdAt;
    }

    public void Revert(decimal quantity, decimal price, TransactionType type)
    {
        if (type == TransactionType.BUY)
        {
            Quantity -= quantity;
            TotalInvestedAmount -= quantity * price;
        }
        else // SELL
        {
            Quantity += quantity;
            TotalInvestedAmount += quantity * price;
        }
    }
}