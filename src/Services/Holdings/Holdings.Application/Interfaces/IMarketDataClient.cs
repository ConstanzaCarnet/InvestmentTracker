using Holdings.Application.DTOs;

namespace Holdings.Application.Interfaces;

public interface IMarketDataClient
{
    Task<Dictionary<Guid, decimal>> GetPricesAsync(List<PriceRequestItem> items);
}