using MarketData.Application.Interfaces;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Http;

namespace MarketData.Infrastructure.Providers;

public class YahooFinancePriceProvider : IPriceProvider
{
    private readonly YahooFinanceClient _client;

    public YahooFinancePriceProvider(YahooFinanceClient client)
    {
        _client = client;
    }

    public async Task<PriceQuote?> GetPriceAsync(string ticker)
    {
        var price = await _client.GetPriceAsync(ticker);

        if (price is null)
            return null;

        return new PriceQuote
        {
            Ticker = ticker.ToUpper(),
            Price = price.Value,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };
    }
}
