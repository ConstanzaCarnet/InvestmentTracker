using MarketData.Application.DTOs;
using MarketData.Application.Interfaces;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Data;
//aqui hacemos mapping de PriceQuote a PriceResponseDto, y también agregamos el método GetPricesAsync para obtener precios de varios tickers al mismo tiempo, aprovechando la caché para evitar llamadas innecesarias al proveedor
public class PriceService : IPriceService
{
    private readonly MarketDataDbContext _context;
    private readonly ICachedPriceService _cachedService;

    public PriceService(
        MarketDataDbContext context,
        ICachedPriceService cachedService)
    {
        _context = context;
        _cachedService = cachedService;
    }

    public async Task<PriceQuote?> GetPriceAsync(string ticker)
    {
        return await _cachedService.GetPriceAsync(ticker);
    }

    public async Task<List<PriceQuote>> GetPricesAsync(List<Guid> instrumentIds)
    {
        var instruments = await _context.Instruments
            .Where(i => instrumentIds.Contains(i.Id))
            .ToListAsync();

        //  1. Deduplicar tickers---> evito múltiples llamadas al proveedor por el mismo ticker si varios instrumentos lo comparten
        var uniqueTickers = instruments
            .Select(i => i.Ticker)
            .Distinct()
            .ToList();

        //  2. Obtener precios UNA sola vez por ticker
        var tickerTasks = uniqueTickers.ToDictionary(
            ticker => ticker,
            ticker => _cachedService.GetPriceAsync(ticker)
        );

        await Task.WhenAll(tickerTasks.Values);

        //  3. Mapear de vuelta a InstrumentId
        var results = new List<PriceQuote>();

        foreach (var instrument in instruments)
        {
            var quote = await tickerTasks[instrument.Ticker];

            if (quote == null)
                continue;

            results.Add(new PriceQuote
            {
                InstrumentId = instrument.Id,
                Ticker = instrument.Ticker,
                Price = quote.Price,
                Currency = quote.Currency,
                Timestamp = quote.Timestamp
            });
        }

        return results;
    }
}