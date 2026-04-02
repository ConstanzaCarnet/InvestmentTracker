using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
using MassTransit;
using Holdings.Infrastructure.Data;
namespace Holdings.API.EventBusConsumer;


public class TransactionCreatedConsumer : IConsumer<TransactionCreatedEvent>
{
    private readonly HoldingsDbContext _context;
    private readonly IHoldingRepository _repository;
    private readonly ILogger<TransactionCreatedConsumer> _logger;

    public TransactionCreatedConsumer(
        HoldingsDbContext context,
        IHoldingRepository repository,
        ILogger<TransactionCreatedConsumer> logger)
    {
        _context = context;
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreatedEvent> context)
    {
        var message = context.Message;

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var ticker = message.Ticker.Trim().ToUpperInvariant();

            var position = await _repository.GetPositionAsync(
                message.UserId,
                message.InstrumentId);

            if (message.Type == TransactionType.BUY)
            {
                if (position == null)
                {
                    position = new Position(
                        message.UserId,
                        message.InstrumentId,
                        ticker
                    );

                    await _repository.AddPositionAsync(position);
                }

                // VALIDACIÓN GLOBAL
                position.ValidateAndApplySequence(
                    message.SequenceNumber,
                    message.CreatedAt
                );

                var lot = position.GetOrCreateLot(message.Currency);

                lot.Buy(
                    message.Quantity,
                    message.Price,
                    message.ConversionRatio,
                    message.ExchangeRate
                );

                _repository.UpdatePosition(position);
            }
            else // SELL
            {
                if (position == null)
                {
                    _logger.LogWarning(
                        "Sell without position. UserId: {UserId}, Ticker: {Ticker}",
                        message.UserId,
                        ticker
                    );
                    return;
                }

                position.ValidateAndApplySequence(
                    message.SequenceNumber,
                    message.CreatedAt
                );

                var lot = position.GetOrCreateLot(message.Currency);

                var shouldDeleteLot = lot.Sell(
                    message.Quantity,
                    message.Price,
                    message.ConversionRatio
                );

                if (shouldDeleteLot)
                    position.RemoveLot(lot);

                if (!position.Lots.Any())
                    _repository.RemovePosition(position);
                else
                    _repository.UpdatePosition(position);
            }

            await _context.SaveChangesAsync(context.CancellationToken);
            await dbTransaction.CommitAsync(context.CancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }
}