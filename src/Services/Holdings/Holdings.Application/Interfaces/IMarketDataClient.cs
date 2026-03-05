namespace Holdings.Application.Interfaces;

/*public record MarketPriceResponse(string Ticker, decimal Price);
//se encargara de comunicarse con otros servicios de mercado de datos externos
public interface IMarketDataClient
{
    //Task<MarketPriceDto?> GetPriceAsync(string ticker);
    Task<MarketPriceResponse?> GetPriceAsync(string ticker);
}
*/

public interface IMarketDataClient
{
    Task<decimal> GetPriceAsync(string ticker);
}