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
            // price = precio de la acción SUBYACENTE (unidad real). El valor de mercado
            // se calcula contra la cantidad real (subyacente), por eso usa TotalRealQuantity.
            var price = prices.GetValueOrDefault(position.InstrumentId);
            var invested = position.TotalInvestedAmount;
            var currentValue = position.TotalRealQuantity * price;
            var pnl = currentValue - invested;

            // Precios por unidad expresados en la MISMA unidad que TotalQuantity (nominal,
            // ej. CEDEARs) para que el balance sea verificable por el cliente:
            //   avgPrice * TotalQuantity == invested  y  currentPrice * TotalQuantity == currentValue
            var avgPurchasePrice = position.TotalQuantity == 0
                ? 0
                : invested / position.TotalQuantity;
            var currentPricePerUnit = position.TotalQuantity == 0
                ? 0
                : currentValue / position.TotalQuantity;

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
                AveragePurchasePrice = avgPurchasePrice,

                CurrentPrice = currentPricePerUnit,
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
