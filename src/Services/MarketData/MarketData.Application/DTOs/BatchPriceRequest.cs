namespace MarketData.Application.DTOs;

public class BatchPriceRequest
{
    public List<Guid> InstrumentIds { get; set; } = new();
}