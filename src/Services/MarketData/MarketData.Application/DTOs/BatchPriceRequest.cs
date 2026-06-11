namespace MarketData.Application.DTOs;

public class BatchPriceRequest
{
    public List<PriceRequestItem> Items { get; set; } = new();
}

public class PriceRequestItem
{
    public Guid InstrumentId { get; set; }
    public string Ticker { get; set; } = string.Empty;
}