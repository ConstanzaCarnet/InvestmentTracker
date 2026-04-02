namespace Holdings.Application.DTOs;

public class BatchPriceResponse
{
    public List<PriceDto> Prices { get; set; } = new();
}
