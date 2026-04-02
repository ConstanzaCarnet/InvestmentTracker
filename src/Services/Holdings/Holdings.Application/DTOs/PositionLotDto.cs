namespace Holdings.Application.DTOs;

public class PositionLotDto
{
    public string Currency { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal RealQuantity { get; set; }

    public decimal InvestedAmount { get; set; }
}