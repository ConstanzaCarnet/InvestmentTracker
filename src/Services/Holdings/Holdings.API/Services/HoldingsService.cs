using Holdings.Application.Interfaces;
using Holdings.Application.DTOs;
using Holdings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Holdings.API.Services;

// 1. Asegúrate de que implemente la interfaz que acabamos de crear
public class HoldingsService : IHoldingsService
{
    private readonly HoldingsDbContext _context;

    public HoldingsService(HoldingsDbContext context)
    {
        _context = context;
    }

    public async Task<PortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var positions = await _context.Positions
         .Where(p => p.UserId == userId)
         .ToListAsync();

        var resultPositions = positions.Select(p => new PositionDto
        {
            UserId = p.UserId,
            Ticker = p.Ticker,
            Quantity = p.Quantity,
            AveragePrice = p.AveragePurchasePrice
        }).ToList();

        return new PortfolioDto
        {
            UserId = userId,
            Positions = resultPositions,
            TotalAssetsValue = resultPositions.Sum(p => p.Quantity * p.AveragePrice)
        };
    }


}
