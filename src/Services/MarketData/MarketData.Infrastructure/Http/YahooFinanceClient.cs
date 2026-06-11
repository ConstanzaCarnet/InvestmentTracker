using System.Text.Json;

namespace MarketData.Infrastructure.Http;

public class YahooFinanceClient
{
    private readonly HttpClient _http;

    public YahooFinanceClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<decimal?> GetPriceAsync(string ticker)
    {
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range=1d";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.Add("Accept", "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        var chart = doc.RootElement.GetProperty("chart");

        if (chart.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
            return null;

        var result = chart.GetProperty("result");

        if (result.ValueKind == JsonValueKind.Null || result.GetArrayLength() == 0)
            return null;

        var meta = result[0].GetProperty("meta");

        if (meta.TryGetProperty("regularMarketPrice", out var priceEl))
            return priceEl.GetDecimal();

        return null;
    }
}
