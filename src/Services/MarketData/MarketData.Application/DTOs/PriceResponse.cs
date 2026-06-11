namespace MarketData.Application.DTOs;

public class PriceResponse
{
    public Guid InstrumentId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime Timestamp { get; set; }
}