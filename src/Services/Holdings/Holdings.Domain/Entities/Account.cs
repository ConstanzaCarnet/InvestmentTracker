namespace Holdings.Domain.Entities;

public class Account
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public decimal CashBalance { get; set; } // Temporal
}