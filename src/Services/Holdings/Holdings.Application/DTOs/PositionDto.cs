namespace Holdings.Application.DTOs;
public class PositionDto
{
    public string Ticker { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageSoldPrice { get; set; }
    public decimal AverageBoughtPrice { get; set; }
    public decimal InvestedAmount { get; set; } // Total invertido menos lo vendido, para calcular la ganancia o pérdida
    //public decimal Subtotal => Quantity * AveragePrice; // Valor de la tenencia al precio de compra
    //public string AssetType { get; set; } = string.Empty; // "Stock", "Bond", etc.

    //constructor
    public PositionDto() { }

    //constructor con parámetros
    public PositionDto(Guid userId, string ticker, decimal quantity, decimal averageBoughtPrice, decimal averageSoldPrice, decimal investedAmount)
    {
        UserId = userId;
        Ticker = ticker;
        Quantity = quantity;
        AverageBoughtPrice = averageBoughtPrice;
        AverageSoldPrice = averageSoldPrice;
        InvestedAmount = investedAmount;
    }
}