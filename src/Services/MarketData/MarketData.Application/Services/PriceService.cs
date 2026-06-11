using MarketData.Application.DTOs;
using MarketData.Application.Interfaces;
using MarketData.Domain.Entities;

namespace MarketData.Application.Services;

public class PriceService : IPriceService
{
    private readonly ICachedPriceService _cachedService;

    public PriceService(ICachedPriceService cachedService)
    {
        _cachedService = cachedService;
    }

    public async Task<PriceQuote?> GetPriceAsync(string ticker)
    {
        return await _cachedService.GetPriceAsync(ticker);
    }

    public async Task<List<PriceQuote>> GetPricesAsync(List<PriceRequestItem> items)
    {
        var uniqueTickers = items
            .Select(i => i.Ticker)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();

        var tickerTasks = uniqueTickers.ToDictionary(
            ticker => ticker,
            ticker => _cachedService.GetPriceAsync(ticker)
        );

        await Task.WhenAll(tickerTasks.Values);

        var results = new List<PriceQuote>();

        foreach (var item in items)
        {
            if (!tickerTasks.TryGetValue(item.Ticker, out var task))
                continue;

            var quote = task.Result;
            if (quote == null)
                continue;

            results.Add(new PriceQuote
            {
                InstrumentId = item.InstrumentId,
                Ticker = item.Ticker.ToUpper(),
                Price = quote.Price,
                Currency = quote.Currency,
                Timestamp = quote.Timestamp
            });
        }

        return results;
    }
}
