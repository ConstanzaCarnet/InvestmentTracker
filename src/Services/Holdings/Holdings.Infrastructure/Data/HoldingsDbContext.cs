using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Holdings.Domain.Entities;

namespace Holdings.Infrastructure.Data;

public class HoldingsDbContext : DbContext
{
    public HoldingsDbContext(DbContextOptions<HoldingsDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Position> Positions => Set<Position>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Account>(account =>
        {
            account.HasKey(a => a.AccountId);

            account.HasMany(a => a.Positions)
                   .WithOne(p => p.Account)
                   .HasForeignKey(p => p.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Position>(position =>
        {
            position.HasKey(p => p.Id);
            position.HasIndex(p => new { p.AccountId, p.Ticker })
                    .IsUnique();

            position.Property(p => p.Ticker)
                    .IsRequired()
                    .HasMaxLength(20);

            position.Property(p => p.Quantity)
                    .HasPrecision(18, 6);

            position.Property(p => p.AveragePurchasePrice)
                    .HasPrecision(18, 6);
        });
    }
}