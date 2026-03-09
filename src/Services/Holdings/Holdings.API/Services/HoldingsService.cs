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

    public HoldingsService(HoldingsDbContext context)
    {
        _context = context;
    }

    public async Task<PortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId);

        var resultPositions = await _context.Positions
            .Where(p => p.UserId == userId)
            .AsNoTracking()
            .Select(p => new PositionDto
            {
                UserId = p.UserId,
                Ticker = p.Ticker,
                Quantity = p.Quantity,
                AverageBoughtPrice = p.AverageBoughtPrice,
                AverageSoldPrice = p.AverageSoldPrice,
                InvestedAmount = p.InvestedAmount
            })
            .ToListAsync();

        return new PortfolioDto
        {
            UserId = userId,
            AccountNumber = account?.AccountId.ToString() ?? string.Empty,
            Positions = resultPositions,
            TotalInvestedAmount = resultPositions.Sum(p => p.InvestedAmount)
        };
    }


}
