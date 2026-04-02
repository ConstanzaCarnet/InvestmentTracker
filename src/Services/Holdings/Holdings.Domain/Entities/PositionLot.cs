namespace Holdings.Domain.Entities;
using EventBus.Messages.Events;

public class PositionLot
{
    public Guid Id { get; private set; }

    public Guid PositionId { get; private set; }

    public string Currency { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal RealQuantity { get; private set; }

    public decimal InvestedAmount { get; private set; }      // base
    public decimal InvestedAmountRaw { get; private set; }   // original

    public decimal TotalBoughtQuantity { get; private set; }
    public decimal TotalBoughtAmount { get; private set; }

    public decimal TotalSoldQuantity { get; private set; }
    public decimal TotalSoldAmount { get; private set; }

    // propiedades calculadas para obtener el precio promedio de compra y venta
    public decimal AverageBoughtPrice => TotalBoughtQuantity == 0 ? 0 : TotalBoughtAmount / TotalBoughtQuantity;
    public decimal AverageSoldPrice => TotalSoldQuantity == 0 ? 0 : TotalSoldAmount / TotalSoldQuantity;

    public decimal AveragePurchasePrice => Quantity == 0 ? 0 : InvestedAmount / Quantity;

    //constructor
    private PositionLot() { }

    public PositionLot(string currency)
    {
        Id = Guid.NewGuid();
        Currency = currency;
    }

    //metodos de la entidad(aplicamos diseño DDD)
    public void Buy(decimal quantity, decimal price, decimal conversionRatio, decimal exchangeRate)
    {
        var total = quantity * price;

        Quantity += quantity;
        RealQuantity += quantity / conversionRatio;

        InvestedAmountRaw += total;
        InvestedAmount += total * exchangeRate;

        TotalBoughtQuantity += quantity;
        TotalBoughtAmount += total;
    }

    public bool Sell(decimal quantity, decimal price, decimal conversionRatio)
    {
        if (quantity > Quantity)
            throw new InvalidOperationException("Insufficient quantity");

        var avgPrice = Quantity == 0 ? 0 : InvestedAmount / Quantity;
        var investedReduction = quantity * avgPrice;

        Quantity -= quantity;
        RealQuantity -= quantity / conversionRatio;

        InvestedAmount -= investedReduction;

        TotalSoldQuantity += quantity;
        TotalSoldAmount += quantity * price;

        if (Quantity == 0)
        {
            InvestedAmount = 0;
            RealQuantity = 0;
        }

        return Quantity == 0;
    }

    // metodo para revertir una transacción en caso de que se detecte un error o inconsistencia, como una venta sin suficiente cantidad o un número de secuencia duplicado
    public void Revert(decimal quantity, decimal price, decimal conversionRatio, decimal exchangeRate, TransactionType type)
    {
        var total = quantity * price;

        if (type == TransactionType.BUY)
        {
            Quantity -= quantity;
            RealQuantity -= quantity / conversionRatio;

            InvestedAmountRaw -= total;
            InvestedAmount -= total * exchangeRate;

            TotalBoughtQuantity -= quantity;
            TotalBoughtAmount -= total;
        }
        else
        {
            Quantity += quantity;
            RealQuantity += quantity / conversionRatio;

            InvestedAmountRaw += total;
            InvestedAmount += total * exchangeRate;

            TotalSoldQuantity -= quantity;
            TotalSoldAmount -= total;
        }

        if (Quantity == 0)
        {
            InvestedAmount = 0;
            RealQuantity = 0;
        }
    }
}
