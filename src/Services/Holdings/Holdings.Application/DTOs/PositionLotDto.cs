namespace Holdings.Application.DTOs;

public class PositionLotDto
{
    public string Currency { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal RealQuantity { get; set; }

    /// <summary>Monto invertido en moneda base (ej: ARS)</summary>
    public decimal InvestedAmount { get; set; }
    /// <summary>Monto invertido en la moneda original del lote (ej: USD)</summary>
    public decimal InvestedAmountRaw { get; set; }

    /// <summary>Precio promedio de compra actual (InvestedAmount / Quantity)</summary>
    public decimal AveragePurchasePrice { get; set; }

    public decimal TotalBoughtQuantity { get; set; }
    public decimal TotalBoughtAmount { get; set; }

    public decimal TotalSoldQuantity { get; set; }
    public decimal TotalSoldAmount { get; set; }
}