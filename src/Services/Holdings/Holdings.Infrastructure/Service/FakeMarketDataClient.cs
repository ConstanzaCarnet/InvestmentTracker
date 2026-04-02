using Holdings.Application.Interfaces;

namespace Holdings.Infrastructure.Services;

public class FakeMarketDataClient : IMarketDataClient
{
    public Task<Dictionary<Guid, decimal>> GetPricesAsync(List<Guid> instrumentIds)
    {
        // Simulación simple
        var result = instrumentIds.ToDictionary(
            id => id,
            id => 100m // precio fijo fake
        );

        return Task.FromResult(result);
    }
}