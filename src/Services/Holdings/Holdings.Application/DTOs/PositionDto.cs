namespace Holdings.Application.DTOs;
public class PositionDto
{
    public string Ticker { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Subtotal => Quantity * AveragePrice; // Valor de la tenencia al precio de compra
    //public string AssetType { get; set; } = string.Empty; // "Stock", "Bond", etc.

    //constructor
    public PositionDto() { }

    //constructor con parámetros
    public PositionDto(string ticker, Guid userId, decimal quantity, decimal averagePrice)
    {
        Ticker = ticker;
        UserId = userId;
        Quantity = quantity;
        AveragePrice = averagePrice;
        //AssetType = assetType;
    }
}