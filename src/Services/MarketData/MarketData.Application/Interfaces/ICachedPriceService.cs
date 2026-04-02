using MarketData.Domain.Entities;

namespace MarketData.Application.Interfaces;

public interface ICachedPriceService
{
	Task<PriceQuote?> GetPriceAsync(string ticker);
}