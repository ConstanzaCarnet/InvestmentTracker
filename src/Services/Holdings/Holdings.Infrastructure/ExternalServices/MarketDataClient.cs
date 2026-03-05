using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Holdings.Application.Interfaces;
using Holdings.Infrastructure.ExternalServices.Models; 

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

    public async Task<decimal> GetPriceAsync(string ticker)
    {
        var response = await _httpClient.GetFromJsonAsync<MarketPriceDto>(
            $"api/marketdata/{ticker}");

        if (response == null)
            throw new Exception("No se pudo obtener el precio del activo");

        return response.Price;
    }
}