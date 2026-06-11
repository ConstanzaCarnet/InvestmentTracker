namespace Holdings.Application.DTOs;
public class PositionDto
{
    public Guid InstrumentId { get; set; }
    public string Ticker { get; set; } = null!;

    /// <summary>Moneda principal de la posición (del lote con mayor monto invertido)</summary>
    public string Currency { get; set; } = null!;

    /// <summary>Cantidad nominal (ej: cantidad de CEDEARs)</summary>
    public decimal TotalQuantity { get; set; }
    /// <summary>Cantidad real en acciones subyacentes (ajustada por ConversionRatio)</summary>
    public decimal TotalRealQuantity { get; set; }

    public decimal TotalInvested { get; set; }
    /// <summary>Precio promedio de compra = TotalInvested / TotalRealQuantity</summary>
    public decimal AveragePurchasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }

    public decimal PnL { get; set; }
    public decimal PnLPercentage { get; set; }

    public decimal PortfolioPercentage { get; set; }

    public List<PositionLotDto> Lots { get; set; } = new();

    //constructor
    public PositionDto() { }
    

    //constructor con par�metros
    public PositionDto( Guid instrumentId, string ticker, decimal totalQuantity, decimal totalRealQuantity, decimal totalInvested, decimal currentPrice, decimal currentValue, decimal pnl, decimal pnlPercentage, decimal portfolioPercentage, List<PositionLotDto> lots)
    {
        InstrumentId = instrumentId;
        Ticker = ticker;
        TotalQuantity = totalQuantity;
        TotalRealQuantity = totalRealQuantity;
        TotalInvested = totalInvested;
        CurrentPrice = currentPrice;
        CurrentValue = currentValue;
        PnL = pnl;
        PnLPercentage = pnlPercentage;
        PortfolioPercentage = portfolioPercentage;
        Lots = lots;
    }
}