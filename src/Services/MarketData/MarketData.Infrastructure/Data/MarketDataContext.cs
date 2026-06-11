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
		modelBuilder.Entity<Instrument>(instrument =>
		{
			instrument.HasKey(x => x.Id);
			instrument.HasIndex(x => x.Ticker).IsUnique();
		});
	}
}