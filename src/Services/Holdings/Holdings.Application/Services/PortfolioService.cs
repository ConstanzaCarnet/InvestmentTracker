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
        {
            result.TotalMarketValue = 0;
            result.TotalPnL = 0;
            result.TotalPnLPercentage = 0;
            return result;
        }

        var priceItems = positions
            .Select(p => new PriceRequestItem { InstrumentId = p.InstrumentId, Ticker = p.Ticker })
            .ToList();

        var prices = await _marketData.GetPricesAsync(priceItems);

        decimal availableMarketValue = 0;

        foreach (var position in positions)
        {
            var invested = position.TotalInvestedAmount;

            // price = precio de la acción SUBYACENTE (unidad real). Ausente o <= 0 ==> no disponible
            // (MarketData caído o sin cotización); en ese caso NO fabricamos valor de mercado ni PnL.
            var hasPrice = prices.TryGetValue(position.InstrumentId, out var price) && price > 0;

            // Precio promedio por unidad NOMINAL (ej. CEDEARs): no depende de MarketData,
            // siempre verificable -> avgPrice * TotalQuantity == invested.
            var avgPurchasePrice = position.TotalQuantity == 0
                ? 0
                : invested / position.TotalQuantity;

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
                PriceAvailable = hasPrice,

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

            if (hasPrice)
            {
                // Valor de mercado contra la cantidad real (subyacente). El precio actual por
                // unidad nominal se deriva del valor para que cierre: currentPrice * qty == value.
                var currentValue = position.TotalRealQuantity * price;
                dto.CurrentValue = currentValue;
                dto.CurrentPrice = position.TotalQuantity == 0 ? 0 : currentValue / position.TotalQuantity;
                dto.PnL = currentValue - invested;
                dto.PnLPercentage = invested == 0 ? 0 : (currentValue - invested) / invested * 100;
                availableMarketValue += currentValue;
            }
            else
            {
                // Sin precio: campos de mercado quedan null y el portfolio se marca incompleto.
                result.PricesComplete = false;
            }

            result.Positions.Add(dto);
            result.TotalInvested += invested;
        }

        // Totales de mercado/PnL solo si TODAS las posiciones tienen precio; un total parcial
        // (valor de algunas posiciones contra el costo de todas) no es verificable.
        if (result.PricesComplete)
        {
            result.TotalMarketValue = availableMarketValue;
            result.TotalPnL = availableMarketValue - result.TotalInvested;
            result.TotalPnLPercentage = result.TotalInvested == 0
                ? 0
                : result.TotalPnL / result.TotalInvested * 100;

            foreach (var p in result.Positions)
            {
                p.PortfolioPercentage = availableMarketValue == 0
                    ? 0
                    : (p.CurrentValue ?? 0) / availableMarketValue * 100;
            }
        }

        return result;
    }
}
