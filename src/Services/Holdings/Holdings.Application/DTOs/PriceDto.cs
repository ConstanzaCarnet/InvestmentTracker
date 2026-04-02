namespace Holdings.Application.DTOs;

public class PriceDto
{
    public Guid InstrumentId { get; set; }
    public decimal Price { get; set; }
}