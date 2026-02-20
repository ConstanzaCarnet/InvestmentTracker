namespace Holdings.Application.DTOs;

public class PortfolioDto
{
	public Guid UserId { get; set; }
	public string AccountNumber { get; set; } = string.Empty;
	public decimal CashBalance { get; set; }
	public string Currency { get; set; } = "ARS";

	// Lista de activos detallada
	public List<PositionDto> Assets { get; set; } = new();

	// Resumen totalizador
	public decimal TotalAssetsValue => Assets.Sum(x => x.Subtotal);
	public decimal PortfolioTotalValue => CashBalance + TotalAssetsValue;
}