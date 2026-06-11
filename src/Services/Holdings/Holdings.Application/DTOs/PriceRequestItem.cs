namespace Holdings.Application.DTOs;

public class PriceRequestItem
{
    public Guid InstrumentId { get; set; }
    public string Ticker { get; set; } = string.Empty;
}
