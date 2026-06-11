using Holdings.Application.DTOs;
using Holdings.Application.Interfaces;

namespace Holdings.Application.Services;

public class PortfolioService : IPortfolioService, IHoldingsService
{
    private readonly IHoldingRepository _repository;
    private readonly IMarketDataClient _marketData;

    public PortfolioService(IHoldingRepository repository, IMarketDataClient marketData)
    {
        _repository = repository;
        _marketData = marketData;
    }

    public async Task<PortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var positions = await _repository.GetPortfolioAsync(userId);

        var result = new PortfolioDto { UserId = userId };

        var account = await _repository.GetByIdAsync(userId);
        result.AccountNumber = account?.AccountNumber;

        if (!positions.Any())
            return result;

        var priceItems = positions
            .Select(p => new PriceRequestItem { InstrumentId = p.InstrumentId, Ticker = p.Ticker })
            .ToList();

        var prices = await _marketData.GetPricesAsync(priceItems);

        foreach (var position in positions)
        {
            var price = prices.GetValueOrDefault(position.InstrumentId);
            var invested = position.TotalInvestedAmount;
            var currentValue = position.TotalRealQuantity * price;
            var pnl = currentValue - invested;

            // Moneda principal: el lote con mayor InvestedAmount
            var primaryLot = position.Lots
                .OrderByDescending(l => l.InvestedAmount)
                .FirstOrDefault();

            var dto = new PositionDto
            {
                InstrumentId = position.InstrumentId,
                Ticker = position.Ticker,
                Currency = primaryLot?.Currency ?? "ARS",

                TotalQuantity = position.TotalQuantity,
                TotalRealQuantity = position.TotalRealQuantity,

                TotalInvested = invested,
                AveragePurchasePrice = position.TotalRealQuantity == 0
                    ? 0
                    : invested / position.TotalRealQuantity,

                CurrentPrice = price,
                CurrentValue = currentValue,

                PnL = pnl,
                PnLPercentage = invested == 0 ? 0 : pnl / invested * 100,

                Lots = position.Lots.Select(l => new PositionLotDto
                {
                    Currency = l.Currency,
                    Quantity = l.Quantity,
                    RealQuantity = l.RealQuantity,
                    InvestedAmount = l.InvestedAmount,
                    InvestedAmountRaw = l.InvestedAmountRaw,
                    AveragePurchasePrice = l.AveragePurchasePrice,
                    TotalBoughtQuantity = l.TotalBoughtQuantity,
                    TotalBoughtAmount = l.TotalBoughtAmount,
                    TotalSoldQuantity = l.TotalSoldQuantity,
                    TotalSoldAmount = l.TotalSoldAmount,
                }).ToList()
            };

            result.Positions.Add(dto);
            result.TotalMarketValue += currentValue;
            result.TotalInvested += invested;
        }

        result.TotalPnL = result.TotalMarketValue - result.TotalInvested;
        result.TotalPnLPercentage = result.TotalInvested == 0
            ? 0
            : result.TotalPnL / result.TotalInvested * 100;

        foreach (var p in result.Positions)
        {
            p.PortfolioPercentage = result.TotalMarketValue == 0
                ? 0
                : p.CurrentValue / result.TotalMarketValue * 100;
        }

        return result;
    }
}
