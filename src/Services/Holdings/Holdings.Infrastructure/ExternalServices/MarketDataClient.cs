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

            // Solo precios válidos (> 0). Un instrumento ausente del diccionario
            // significa "precio no disponible", nunca un 0 fabricado: el llamador
            // (PortfolioService) lo reporta como tal en vez de inventar un valor.
            return result!.Prices
                .Where(p => p.Price > 0)
                .ToDictionary(p => p.InstrumentId, p => p.Price);
        }
        catch (Exception ex)
        {
            // Reintentos/timeout ya los maneja la HttpClient policy; si igual falla,
            // devolvemos vacío (precios no disponibles) en lugar de ceros engañosos
            // que mostrarían las posiciones con valor 0 y PnL -100%.
            _logger.LogWarning(ex, "Failed to fetch prices from MarketData. Prices will be reported as unavailable.");
            return new Dictionary<Guid, decimal>();
        }
    }
}
