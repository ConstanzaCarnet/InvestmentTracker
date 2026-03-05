using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Holdings.Domain.Entities;

namespace Holdings.Infrastructure.Data;

public class HoldingsDbContext : DbContext
{
    public HoldingsDbContext(DbContextOptions<HoldingsDbContext> options)
        : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Position> Positions => Set<Position>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // -----------------------
        // ACCOUNT CONFIG
        // -----------------------
        builder.Entity<Account>(account =>
        {
            account.HasKey(a => a.AccountId);

            account.Property(a => a.UserId)
                   .IsRequired();

            account.Property(a => a.CashBalance)
                   .HasPrecision(18, 4);

            // Un usuario = una cuenta
            account.HasIndex(a => a.UserId)
                   .IsUnique();
        });

        // -----------------------
        // POSITION CONFIG
        // -----------------------
        builder.Entity<Position>(position =>
        {
            position.HasKey(p => p.Id);

            position.Property(p => p.UserId)
                    .IsRequired();

            position.Property(p => p.Ticker)
                    .IsRequired();

            position.Property(p => p.Quantity)
                    .HasPrecision(18, 4);

            position.Property(p => p.AveragePurchasePrice)
                    .HasPrecision(18, 4);

            position.Property(p => p.LastProcessedSequenceNumber)
                    .IsRequired();
            //se relaciona con Account de manera indirecta, mediante UserId, para evitar problemas de concurrencia y bloqueo al actualizar posiciones
            position.HasIndex(p => new { p.UserId, p.Ticker })
                    .IsUnique();
        });
    }
}