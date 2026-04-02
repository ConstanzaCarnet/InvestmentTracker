namespace Holdings.Application.DTOs;

public class PortfolioDto
{
	public Guid UserId { get; set; }
	public string AccountNumber { get; set; } = string.Empty;
    public List<PositionDto> Positions { get; set; } = new();
    //public decimal CashBalance { get; set; }

    public decimal TotalInvestedAmount { get; set; }
    public decimal TotalMarketValue { get; set; }
    public decimal TotalPnL { get; set; }
}