using Portfolio.Domain.Interfaces;

namespace Holdings.API.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IHoldingRepository _repository;
    private readonly IMarketDataClient _marketData;

    public PortfolioService(
        IHoldingRepository repository,
        IMarketDataClient marketData)
    {
        _repository = repository;
        _marketData = marketData;
    }

    public async Task<PortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var positions = await _repository.GetPortfolioAsync(userId);

        if (!positions.Any())
            return new PortfolioDto();

        // pedir precios en batch
        var tickers = positions.Select(p => p.Ticker).ToList();
        var prices = await _marketData.GetPricesAsync(tickers);

        var result = new PortfolioDto();

        foreach (var position in positions)
        {
            var price = prices.GetValueOrDefault(position.Ticker);

            var currentValue = position.TotalRealQuantity * price;
            var invested = position.TotalInvestedAmount;

            var pnl = currentValue - invested;

            var dto = new PositionDto
            {
                InstrumentId = position.InstrumentId,
                Ticker = position.Ticker,

                TotalQuantity = position.TotalQuantity,
                TotalRealQuantity = position.TotalRealQuantity,

                TotalInvestedAmount = invested,
                CurrentPrice = price,
                CurrentValue = currentValue,

                PnL = pnl,
                PnLPercentage = invested == 0 ? 0 : pnl / invested * 100,

                Lots = position.Lots.Select(l => new PositionLotDto
                {
                    Currency = l.Currency,
                    Quantity = l.Quantity,
                    RealQuantity = l.RealQuantity,
                    InvestedAmount = l.InvestedAmount
                }).ToList()
            };

            result.Positions.Add(dto);

            result.TotalValue += currentValue;
            result.TotalInvested += invested;
        }

        result.TotalPnL = result.TotalValue - result.TotalInvested;

        // calcular % del portfolio---> es decir, porcentaje del instrumento en el portfolio
        foreach (var p in result.Positions)
        {
            p.PortfolioPercentage = result.TotalMarketValue == 0
                ? 0
                : (p.CurrentValue / result.TotalMarketValue) * 100;
        }

        return result;
    }
}