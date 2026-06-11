using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Holdings.Application.Interfaces;
using Holdings.Application.DTOs;

namespace Holdings.Infrastructure.ExternalServices;

public class MarketDataClient : IMarketDataClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MarketDataClient> _logger;

    public MarketDataClient(HttpClient httpClient, ILogger<MarketDataClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Dictionary<Guid, decimal>> GetPricesAsync(List<PriceRequestItem> items)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/prices/batch",
                new { items });

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BatchPriceResponse>();

            return result!.Prices.ToDictionary(p => p.InstrumentId, p => p.Price);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch prices from MarketData. Returning zero prices.");
            return items.ToDictionary(i => i.InstrumentId, i => 0m);
        }
    }
}
