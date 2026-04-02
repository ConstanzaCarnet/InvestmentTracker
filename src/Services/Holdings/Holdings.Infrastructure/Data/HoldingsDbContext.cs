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
    public DbSet<PositionLot> PositionLots => Set<PositionLot>();


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
            
            position.HasIndex(p => new { p.UserId, p.InstrumentId })
                    .IsUnique();

            position.Property(p => p.InstrumentId)
                    .IsRequired();

            position.Property(p => p.Ticker)
                    .IsRequired();
            //agregamos los lots
            position.HasMany(p => p.Lots)
                    .WithOne()
                    .HasForeignKey(l => l.PositionId)
                    .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PositionLot>(lot =>
        {
            lot.HasKey(l => l.Id);
            //aclaramos PositionId como foreign key, aunque EF lo infiere por la convención, es para dejarlo explícito y claro
            lot.HasOne<Position>()
                .WithMany(p => p.Lots)
                .HasForeignKey(l => l.PositionId)
                .OnDelete(DeleteBehavior.Cascade);

            lot.Property(l => l.PositionId)
                .IsRequired();

            lot.Property(l => l.Currency)
                .IsRequired();

            lot.Property(l => l.Quantity)
                .HasPrecision(18, 4);
            lot.Property(l => l.RealQuantity)
                .HasPrecision(18, 4);
            lot.Property(l => l.InvestedAmount)
                .HasPrecision(18, 4);
            lot.Property(l => l.InvestedAmountRaw)
                .HasPrecision(18, 4);
            lot.Property(l => l.TotalBoughtQuantity)
                .HasPrecision(18, 4);
            lot.Property(l => l.TotalBoughtAmount)
                .HasPrecision(18, 4);
            lot.Property(l => l.TotalSoldQuantity)
                .HasPrecision(18, 4);
            lot.Property(l => l.TotalSoldAmount)
                .HasPrecision(18, 4);


            lot.Ignore(p => p.AveragePurchasePrice);
            lot.Ignore(p => p.AverageBoughtPrice);
            lot.Ignore(p => p.AverageSoldPrice);
        });
    }
}