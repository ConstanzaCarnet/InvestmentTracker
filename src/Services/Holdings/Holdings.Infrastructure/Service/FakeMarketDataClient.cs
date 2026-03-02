using Holdings.Application.Interfaces;

namespace Holdings.Infrastructure.Services;

public class FakeMarketDataClient : IMarketDataClient
{
    public Task<decimal> GetPriceAsync(string ticker)
    {
        // Simulamos precios distintos según ticker
        var price = ticker.ToUpper() switch
        {
            "AAPL" => 180m,
            "TSLA" => 250m,
            "MSFT" => 330m,
            _ => 100m
        };

        return Task.FromResult(price);
    }
}