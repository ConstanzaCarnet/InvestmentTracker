using MarketData.Domain.Entities;

namespace MarketData.Application.Interfaces;

public interface IPriceProvider
{
	Task<PriceQuote?> GetPriceAsync(string ticker);
}