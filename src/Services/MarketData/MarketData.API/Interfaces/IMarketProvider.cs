using MarketData.API.Entities;
namespace MarketData.API.Interfaces;
//se encargara de proporcionar precios de mercado para diferentes activos financieros, al comunicarse con proveedores externos.(APIs externas)
public interface IMarketProvider
{
    Task<MarketPrice> GetPriceAsync(string ticker);
}