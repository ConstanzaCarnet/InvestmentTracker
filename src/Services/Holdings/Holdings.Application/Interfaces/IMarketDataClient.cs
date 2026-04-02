namespace Holdings.Application.Interfaces;

public interface IMarketDataClient
{
    Task<Dictionary<Guid, decimal>> GetPricesAsync(List<Guid> instrumentIds);
}