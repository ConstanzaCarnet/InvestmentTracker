using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Transactions.Domain.Entities;
using Transactions.Infrastructure.Outbox;

namespace Transactions.Infrastructure.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var transaction = modelBuilder.Entity<Transaction>();

        transaction.Property(t => t.Price)
                   .HasPrecision(18, 4);

        transaction.Property(t => t.Quantity)
                   .HasPrecision(18, 4);

        transaction.Property(t => t.ExchangeRate)
                    .HasPrecision(18, 6); 

        transaction.Property(t => t.Currency)
                   .HasConversion<string>() 
                   .HasMaxLength(10);

        transaction.HasIndex(t => new { t.UserId, t.Ticker, t.Version })
                   .IsUnique();

        base.OnModelCreating(modelBuilder);
    }

}