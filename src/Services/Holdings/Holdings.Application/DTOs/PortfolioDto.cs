namespace Holdings.Application.DTOs;

public class PortfolioDto
{
    public Guid UserId { get; set; }
    public string? AccountNumber { get; set; }
    public List<PositionDto> Positions { get; set; } = new();

    public decimal TotalInvested { get; set; }
    public decimal TotalMarketValue { get; set; }
    public decimal TotalPnL { get; set; }
    /// <summary>Rentabilidad total del portfolio = TotalPnL / TotalInvested * 100</summary>
    public decimal TotalPnLPercentage { get; set; }
}