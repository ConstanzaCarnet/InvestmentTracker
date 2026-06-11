using System.Globalization;
using CsvHelper;
using MarketData.Domain.Entities;

namespace MarketData.Infrastructure.Data;

public static class InstrumentsSeeder
{
    public static async Task SeedAsync(MarketDataContext context)
    {
        if (context.Instruments.Any())
            return;

        var path = Path.Combine("Data", "Instruments_db.csv");

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<InstrumentCsv>().ToList();

        var instruments = records.Select(r => new Instrument
        {
            Id = Guid.NewGuid(),
            Ticker = r.Ticker,
            Name = r.Name,
            Exchange = r.Exchange,
            ConversionRatio = r.ConversionRatio,
            Sector = r.Sector,
            Region = r.Region,
            AssetType = r.AssetType,
            IsActive = r.IsActive
        });

        context.Instruments.AddRange(instruments);

        await context.SaveChangesAsync();
    }
}