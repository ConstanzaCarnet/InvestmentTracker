using System.Transactions;
using Microsoft.EntityFrameworkCore;
using MarketData.Domain.Entities;

namespace MarketData.Infrastructure.Data;

public class MarketDataContext : DbContext
{
	public MarketDataContext(DbContextOptions<MarketDataContext> options) : base(options)
	{
	}
	public DbSet<Instrument> Instruments => Set<Instrument>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		// Configuración de la entidad Price
		modelBuilder.Entity<Instruments>(Instruments =>
		{
			Instruments.HasKey(x => x.Id);
			Instruments.HasIndex(x => x.Ticker).IsUnique();
		});
	}
}