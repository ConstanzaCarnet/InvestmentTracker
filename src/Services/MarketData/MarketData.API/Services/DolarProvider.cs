using MarketData.API.Interfaces;
using MarketData.API.Entities;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MarketData.API.Services;

public class DolarProvider : IMarketProvider
{
    private readonly HttpClient _httpClient;

    public DolarProvider(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<MarketPrice> GetPriceAsync(string ticker)
    {
        // Ejemplo consultando DolarApi.com (Gratis y sin Token)
        // ticker puede ser "mep", "oficial", "blue"
        var response = await _httpClient.GetFromJsonAsync<DolarApiResponse>($"https://dolarapi.com/v1/dolares/{ticker}");

        return new MarketPrice(
            ticker: response.Nombre,
            price: response.Compra, // O 'Venta' según prefieras
            currency: "ARS",
            dateTime: response.FechaActualizacion,
            openPrice: null
        );
    }
}


/*public record DolarApiResponse(
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("compra")] decimal Compra,
    [property: JsonPropertyName("venta")] decimal Venta,
    [property: JsonPropertyName("fechaActualizacion")] DateTime FechaActualizacion
);*/

public record DolarApiResponse(string Nombre, decimal Compra, decimal Venta, DateTime FechaActualizacion);