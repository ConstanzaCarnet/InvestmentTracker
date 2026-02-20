namespace Holdings.Application.DTOs;
public class PositionDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Subtotal => Quantity * AveragePrice; // Valor de la tenencia al precio de compra
    public string AssetType { get; set; } = string.Empty;
}