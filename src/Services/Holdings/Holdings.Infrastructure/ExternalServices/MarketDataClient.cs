using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Holdings.Application.Interfaces;

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

    public async Task<MarketPriceResponse?> GetPriceAsync(string ticker)
    {
        try
        {
            // La URL base se configura en el Program.cs
            return await _httpClient.GetFromJsonAsync<MarketPriceResponse>($"api/market/{ticker}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar con MarketData para {Ticker}", ticker);
            return null;
        }
    }
}