using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Holdings.Infrastructure.Data;

public class HoldingsDbContextFactory : IDesignTimeDbContextFactory<HoldingsDbContext>
{
    public HoldingsDbContext CreateDbContext(string[] args)
    {
        // Ubicación del proyecto API (donde está appsettings.json)
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../Holdings.API");

        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<HoldingsDbContext>();

        var connectionString = config.GetConnectionString("DefaultConnection");

        optionsBuilder.UseSqlServer(connectionString);

        return new HoldingsDbContext(optionsBuilder.Options);
    }
}
