using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Holdings.Application.Interfaces;
using Holdings.Infrastructure.ExternalServices.Models; 
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

    public async Task<Dictionary<Guid, decimal>> GetPricesAsync(List<Guid> instrumentIds)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/prices/batch",
                new { instrumentIds });

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BatchPriceResponse>();

            return result!.Prices.ToDictionary(p => p.InstrumentId, p => p.Price);
        }
        catch
        {
            return instrumentIds.ToDictionary(i => i, i => 0m);
        }
    }
}