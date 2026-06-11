using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
using Holdings.Infrastructure.Data;
using Holdings.Application.Helpers;
using MassTransit;

namespace Holdings.API.EventBusConsumer;

public class TransactionDeletedConsumer : IConsumer<TransactionDeletedEvent>
{
    private readonly HoldingsDbContext _context;
    private readonly IHoldingRepository _repository;
    private readonly ILogger<TransactionDeletedConsumer> _logger;

    public TransactionDeletedConsumer(
        HoldingsDbContext context,
        IHoldingRepository repository,
        ILogger<TransactionDeletedConsumer> logger)
    {
        _context = context;
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionDeletedEvent> context)
    {
        var message = context.Message;

        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var ticker = message.Ticker.Trim().ToUpperInvariant();
            var mappedType = MapTransactionType(message.Type);

            var position = await _repository.GetPositionAsync(
                message.UserId,
                message.InstrumentId);

            if (position == null)
            {
                _logger.LogWarning(
                    "Delete event but no position found User={User} Ticker={Ticker}",
                    message.UserId,
                    ticker);
                return;
            }

            // 🔥 VALIDACIÓN
            position.ValidateAndApplySequence(
                message.SequenceNumber,
                message.CreatedAt
            );

            var lot = position.GetOrCreateLot(message.Currency);
            lot.Revert(
                message.Quantity,
                message.Price,
                message.ConversionRatio,
                message.ExchangeRate,
                mappedType
            );

            if (lot.Quantity == 0)
                position.RemoveLot(lot);

            if (!position.Lots.Any())
                _repository.RemovePosition(position);
            else
                _repository.UpdatePosition(position);

            await _context.SaveChangesAsync(context.CancellationToken);
            await dbTransaction.CommitAsync(context.CancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }

    //mapeo explicito para enum
    private Holdings.Domain.Enums.TransactionType MapTransactionType(
     EventBus.Messages.Events.TransactionType type)
    {
        return type switch
        {
            EventBus.Messages.Events.TransactionType.BUY
                => Holdings.Domain.Enums.TransactionType.BUY,

            EventBus.Messages.Events.TransactionType.SELL
                => Holdings.Domain.Enums.TransactionType.SELL,

            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}