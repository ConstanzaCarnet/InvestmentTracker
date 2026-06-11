using Holdings.Domain.Enums;

namespace Holdings.Domain.Entities;
//"la posición de un inversor sobre un instrumento"
public class Position
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid InstrumentId { get; set; }
    public string Ticker { get; private set; }

    public List<PositionLot> Lots { get; private set; } = new();

    public long LastProcessedSequenceNumber { get; private set; }
    public DateTime LastTransactionDate { get; private set; }

    // Totales consolidados (lo que quiere el cliente)
    public decimal TotalQuantity => Lots.Sum(x => x.Quantity);
    public decimal TotalRealQuantity => Lots.Sum(x => x.RealQuantity);
    public decimal TotalInvestedAmount => Lots.Sum(x => x.InvestedAmount);

    //constructores
    private Position() { }

    public Position(Guid userId, Guid instrumentId, string ticker)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        InstrumentId = instrumentId;
        Ticker = ticker;
    }

    // metodo para obtener o crear un lote específico para una moneda determinada, lo que permite manejar múltiples lotes dentro de la misma posición, cada uno con su propia moneda y detalles de inversión
    public PositionLot GetOrCreateLot(string currency)
    {
        var lot = Lots.FirstOrDefault(x => x.Currency == currency);

        if (lot == null)
        {
            lot = new PositionLot(currency);
            Lots.Add(lot);
        }

        return lot;
    }

    public void RemoveLot(PositionLot lot)
    {
        Lots.Remove(lot);
    }

    public void ValidateAndApplySequence(long sequence, DateTime createdAt)
    {
        if (sequence <= LastProcessedSequenceNumber)
            throw new InvalidOperationException($"Duplicate or old event. Received: {sequence}, Last processed: {LastProcessedSequenceNumber}");

        LastProcessedSequenceNumber = sequence;
        LastTransactionDate = createdAt;
    }
 
}



/*Refactorización a futuro
public class Position
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Ticker { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal TotalInvestedAmount { get; private set; }

    public long LastProcessedSequenceNumber { get; private set; }

    public decimal AveragePurchasePrice =>
        Quantity == 0 ? 0 : TotalInvestedAmount / Quantity;

    protected Position() { }

    public Position(Guid userId, string ticker)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Ticker = ticker;
        Quantity = 0;
        TotalInvestedAmount = 0;
    }

    private void ApplyChange(decimal quantityDelta, decimal investedDelta)
    {
        Quantity += quantityDelta;
        TotalInvestedAmount += investedDelta;

        if (Quantity < 0)
            throw new InvalidOperationException("Position quantity cannot be negative.");

        if (TotalInvestedAmount < 0)
            TotalInvestedAmount = 0;
    }

    public void Buy(decimal quantity, decimal price, long sequenceNumber, DateTime createdAt)
    {
        var invested = quantity * price;

        ApplyChange(quantity, invested);

        LastProcessedSequenceNumber = sequenceNumber;
    }

    public bool Sell(decimal quantity, long sequenceNumber, DateTime createdAt)
    {
        if (quantity > Quantity)
            throw new InvalidOperationException("Cannot sell more than current position.");

        var avgPrice = AveragePurchasePrice;
        var investedReduction = quantity * avgPrice;

        ApplyChange(-quantity, -investedReduction);

        LastProcessedSequenceNumber = sequenceNumber;

        return Quantity == 0;
    }

    public void Revert(decimal quantity, decimal price, TransactionType type)
    {
        var invested = quantity * price;

        if (type == TransactionType.BUY)
            ApplyChange(-quantity, -invested);
        else
            ApplyChange(quantity, invested);
    }
}
*/