using MarketData.Application.Interfaces;
using MarketData.Domain.Entities;

namespace MarketData.Infrastructure.Providers;

public class FakePriceProvider : IPriceProvider
{
    public Task<PriceQuote?> GetPriceAsync(string ticker)
    {
        var price = new PriceQuote
        {
            Ticker = ticker.ToUpper(),
            Price = Random.Shared.Next(50, 500),
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        return Task.FromResult<PriceQuote?>(price);
    }
}