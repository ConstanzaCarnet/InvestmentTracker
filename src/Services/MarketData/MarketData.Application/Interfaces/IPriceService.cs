using MarketData.Application.DTOs;
using MarketData.Domain.Entities;

namespace MarketData.Application.Interfaces;
public interface IPriceService
{
    Task<PriceQuote?> GetPriceAsync(string ticker);
    Task<List<PriceQuote>> GetPricesAsync(List<PriceRequestItem> items);
}