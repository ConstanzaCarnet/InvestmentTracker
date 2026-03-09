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
            //agregamos AcountNumber como propiedad requerida y única---> se usará mas que nada para mostrarla al usuario, el Id es el que se usará para las relaciones y para la lógica interna
            account.Property(a => a.AccountNumber)
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
            position.Property(p => p.InvestedAmount)
                    .HasPrecision(18, 4);
            position.Property(p => p.TotalBoughtAmount)
                    .HasPrecision(18, 4);
            position.Property(p => p.TotalSoldAmount)
                    .HasPrecision(18, 4);
            position.Property(p => p.TotalBoughtQuantity)
                    .HasPrecision(18, 4);
            position.Property(p => p.TotalSoldQuantity)
                    .HasPrecision(18, 4);
            position.Property(p => p.LastProcessedSequenceNumber)
                    .IsRequired();
            position.HasIndex(p => new { p.UserId, p.Ticker })
                    .IsUnique();

            position.Ignore(p => p.AveragePurchasePrice);
            position.Ignore(p => p.AverageBoughtPrice);
            position.Ignore(p => p.AverageSoldPrice);
        });
    }
}