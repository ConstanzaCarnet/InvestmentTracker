using MarketData.Application.Interfaces;
using MarketData.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace MarketData.Application.Services;

public class CachedPriceService : ICachedPriceService
{
    private readonly IMemoryCache _cache;
    private readonly IPriceProvider _provider;

    public CachedPriceService(
        IMemoryCache cache,
        IPriceProvider provider)
    {
        _cache = cache;
        _provider = provider;
    }

    public async Task<PriceQuote?> GetPriceAsync(string ticker)
    {
        if (_cache.TryGetValue(ticker, out PriceQuote cached))
            return cached;

        var price = await _provider.GetPriceAsync(ticker);

        if (price != null)
        {
            _cache.Set(ticker, price, TimeSpan.FromSeconds(120));
        }

        return price;
    }
}