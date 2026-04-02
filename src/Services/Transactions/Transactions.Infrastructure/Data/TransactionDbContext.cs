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
    public DbSet<Instrument> Instruments => Set<Instrument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //entidad principal Transaction
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
        //entidad OutboxMessage, guardará los mensajes de eventos que se publicarán posteriormente, para garantizar la consistencia eventual entre los microservicios
        modelBuilder.Entity<OutboxMessage>(outbox =>
        {
            outbox.HasKey(o => o.Id);

            outbox.HasIndex(o => o.ProcessedOnUtc);

            outbox.Property(o => o.Type)
                  .HasMaxLength(200);
        });
        //entidad Instrument, guardará la información de los instrumentos financieros, como su ticker, nombre, exchange, sector, región, etc. para evitar depender de servicios externos para obtener esta información
        modelBuilder.Entity<Instrument>(Instruments =>
        {
            Instruments.HasKey(i => i.Id);

            Instruments.HasIndex(i => i.Ticker)
                       .IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }

}