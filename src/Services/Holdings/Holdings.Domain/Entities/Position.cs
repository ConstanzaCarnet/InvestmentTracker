using Holdings.Domain.Enums;

namespace Holdings.Domain.Entities;
//"la posición de un inversor sobre un instrumento"
public class Position
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Ticker { get; private set; }
    public decimal Quantity { get; private set; }
    // dinero neto invertido
    public decimal InvestedAmount { get; private set; }
    // acumuladores para luego calcular promedios correspondientes
    public decimal TotalBoughtAmount { get; private set; }
    public decimal TotalSoldAmount { get; private set; }
    
    public decimal TotalBoughtQuantity { get; private set; }
    public decimal TotalSoldQuantity { get; private set; }
    // propiedades calculadas para obtener el precio promedio de compra y venta
    public decimal AverageBoughtPrice => TotalBoughtQuantity == 0 ? 0 : TotalBoughtAmount / TotalBoughtQuantity;
    public decimal AverageSoldPrice => TotalSoldQuantity == 0 ? 0 : TotalSoldAmount / TotalSoldQuantity;

    public decimal AveragePurchasePrice => Quantity == 0 ? 0 : InvestedAmount / Quantity;
    // control de lectura de los mensajes, asi evitamos que se lean en cualquier orden, y que se procesen mensajes viejos o duplicados
    public long LastProcessedSequenceNumber { get; private set; }
    public DateTime LastTransactionDate { get; private set; }

    //constructores
    private Position() { }

    public Position(Guid userId, string ticker)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Ticker = ticker;
        Quantity = 0;
        InvestedAmount = 0;
        TotalBoughtAmount = 0;
        TotalSoldAmount = 0;
        TotalBoughtQuantity = 0;
        TotalSoldQuantity = 0;
    }
    //metodos de la entidad
    public void Buy(decimal quantity, decimal price, long sequence, DateTime createdAt)
    {
        ValidateSequence(sequence);

        var totalCost = quantity * price;

        Quantity += quantity;

        InvestedAmount += totalCost;

        TotalBoughtQuantity += quantity;
        TotalBoughtAmount += totalCost;

        ApplyMetadata(sequence, createdAt);
    }

    public bool Sell(decimal quantity, decimal price, long sequence, DateTime createdAt)
    {
        ValidateSequence(sequence);

        if (quantity > Quantity)
            throw new InvalidOperationException("Insufficient quantity");

        var totalSell = quantity * price;

        Quantity -= quantity;

        InvestedAmount -= totalSell;

        TotalSoldQuantity += quantity;
        TotalSoldAmount += totalSell;

        ApplyMetadata(sequence, createdAt);

        return Quantity == 0;
    }
    // metodo para revertir una transacción en caso de que se detecte un error o inconsistencia, como una venta sin suficiente cantidad o un número de secuencia duplicado
    public void Revert(decimal quantity, decimal price, TransactionType type)
    {
        var total = quantity * price;

        if (type == TransactionType.BUY)
        {
            Quantity -= quantity;
            InvestedAmount -= total;

            TotalBoughtQuantity -= quantity;
            TotalBoughtAmount -= total;
        }
        else // SELL
        {
            Quantity += quantity;
            InvestedAmount += total;

            TotalSoldQuantity -= quantity;
            TotalSoldAmount -= total;
        }
    }
    //logica interna
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