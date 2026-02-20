using Transactions.Domain.Enums;

namespace Transactions.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }

    public long Version { get; set; }


    // Constructor vacío para EF Core
    public Transaction() { }

    // Constructor que usas en el Service
    public Transaction(Guid userId, string ticker, decimal quantity, decimal price, TransactionType type, long version)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Ticker = ticker;
        Quantity = quantity;
        Price = price;
        Type = type;
        Version = version;
        CreatedAt = DateTime.UtcNow;
    }
}

