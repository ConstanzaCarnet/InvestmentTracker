namespace MarketData.Application.DTOs;

public class BatchPriceResponse
{
    public List<PriceResponse> Prices { get; set; } = new();
}