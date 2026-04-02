using Holdings.Application.Interfaces;
using Holdings.Application.DTOs;
using Holdings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Holdings.Domain.Entities;

namespace Holdings.API.Services;

// 1. Asegúrate de que implemente la interfaz que acabamos de crear
public class HoldingsService : IHoldingsService
{
    private readonly HoldingsDbContext _context;
    private readonly IMarketDataClient _marketDataClient;

    public HoldingsService(
        HoldingsDbContext context,
        IMarketDataClient marketDataClient)
    {
        _context = context;
        _marketDataClient = marketDataClient;
    }

    public async Task<PortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var positions = await _context.Positions
            .Where(p => p.UserId == userId)
            .AsNoTracking()
            .ToListAsync();

        // 1. obtener tickers
        var tickers = positions.Select(p => p.Ticker).Distinct().ToList();

        // 2. pedir precios a MarketData
        var prices = await _marketDataClient.GetPricesAsync(tickers);

        var resultPositions = new List<PositionDto>();

        foreach (var p in positions)
        {
            var currentPrice = prices.GetValueOrDefault(p.Ticker, 0);
            // 3. calcular valor de mercado y PnL
            var realQty = p.RealQuantity;
            var marketValue = realQty * currentPrice;
            var pnl = marketValue - p.InvestedAmount;
            // 4. mapear a PositionDto
            resultPositions.Add(new PositionDto
            {
                UserId = p.UserId,
                Ticker = p.Ticker,

                Quantity = p.Quantity,

                AverageBoughtPrice = p.AverageBoughtPrice,
                AverageSoldPrice = p.AverageSoldPrice,

                InvestedAmount = p.InvestedAmount,

                CurrentPrice = currentPrice,
                MarketValue = marketValue,
                UnrealizedPnL = pnl
            });
        }
        // 5. mapear a PortfolioDto
        return new PortfolioDto
        {
            UserId = userId,
            AccountNumber = account?.AccountId.ToString() ?? string.Empty,
            Positions = resultPositions,

            TotalInvestedAmount = resultPositions.Sum(p => p.InvestedAmount),
            TotalMarketValue = resultPositions.Sum(p => p.MarketValue),
            TotalPnL = resultPositions.Sum(p => p.UnrealizedPnL)
        };
    }


}
