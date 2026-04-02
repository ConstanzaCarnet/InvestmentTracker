using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MarketData.Application.Interfaces;
using MarketData.Domain.Entities;

namespace MarketData.Infrastructure.Http;

public class FinnhubClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public FinnhubClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Finnhub:ApiKey"]!;
    }

    public async Task<decimal?> GetPriceAsync(string ticker)
    {
        var url = $"https://finnhub.io/api/v1/quote?symbol={ticker}&token={_apiKey}";

        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);

        var price = doc.RootElement.GetProperty("c").GetDecimal();

        return price;
    }
}