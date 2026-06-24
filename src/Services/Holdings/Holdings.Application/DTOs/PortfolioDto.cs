namespace Holdings.Application.DTOs;

public class PortfolioDto
{
    public Guid UserId { get; set; }
    public string? AccountNumber { get; set; }
    public List<PositionDto> Positions { get; set; } = new();

    /// <summary>Costo total invertido. Siempre disponible (no depende de MarketData).</summary>
    public decimal TotalInvested { get; set; }

    /// <summary>true si se obtuvo el precio de TODAS las posiciones. Si es false, los totales de mercado/PnL quedan null (parciales no son verificables).</summary>
    public bool PricesComplete { get; set; } = true;

    /// <summary>Valor de mercado total. null si falta el precio de alguna posición (ver PricesComplete).</summary>
    public decimal? TotalMarketValue { get; set; }
    public decimal? TotalPnL { get; set; }
    /// <summary>Rentabilidad total del portfolio = TotalPnL / TotalInvested * 100. null si faltan precios.</summary>
    public decimal? TotalPnLPercentage { get; set; }
}