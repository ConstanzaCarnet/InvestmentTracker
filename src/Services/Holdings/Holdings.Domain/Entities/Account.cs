namespace Holdings.Domain.Entities;

public class Account
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string? AccountNumber { get; private set; }
    public decimal CashBalance { get; set; } // Temporal

    public Account(Guid userId)
    {
        AccountId = Guid.NewGuid();
        UserId = userId;
        AccountNumber = GenerateAccountNumber();
    }

    private static string GenerateAccountNumber()
    {
        return $"INV-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}