using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
using Holdings.Infrastructure.Data;
using Holdings.Application.Helpers;
using MassTransit;

namespace Holdings.API.EventBusConsumer;

public class TransactionUpdatedConsumer : IConsumer<TransactionUpdatedEvent>
{
    private readonly HoldingsDbContext _context;
    private readonly IHoldingRepository _repository;
    private readonly ILogger<TransactionUpdatedConsumer> _logger;

    public TransactionUpdatedConsumer(
        HoldingsDbContext context,
        IHoldingRepository repository,
        ILogger<TransactionUpdatedConsumer> logger)
    {
        _context = context;
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionUpdatedEvent> context)
    {
        var message = context.Message;
        
        var type = TransactionTypeMapper.ToDomain(message.Type);

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var newTicker = message.Ticker.Trim().ToUpperInvariant();
            var oldTicker = message.PreviousTicker.Trim().ToUpperInvariant();
            //revisamos si el ticker cambió para saber si debemos revertir en la misma posición o en otra
            var tickerChanged = !string.Equals(newTicker, oldTicker, StringComparison.OrdinalIgnoreCase);

            // 1️ Revertimos la posición anterior
            var oldPosition = await _repository.GetPositionAsync(message.UserId, oldTicker);

            if (oldPosition != null)
            {
                oldPosition.Revert(
                    message.PreviousQuantity,
                    message.PreviousPrice,
                    type);

                if (oldPosition.Quantity <= 0)
                    _repository.RemovePosition(oldPosition);
                else
                    _repository.UpdatePosition(oldPosition);
            }

            // 2️ Si el ticker cambió debemos aplicar en otra posición
            var targetTicker = tickerChanged ? newTicker : oldTicker;

            var position = await _repository.GetPositionAsync(message.UserId, targetTicker);

            if (position == null)
            {
                position = new Position(message.UserId, targetTicker);
                await _repository.AddPositionAsync(position);
            }

            if (message.Type == TransactionType.BUY)
            {
                position.Buy(
                    message.Quantity,
                    message.Price,
                    message.SequenceNumber,
                    message.CreatedAt);
            }
            else
            {
                var shouldDelete = position.Sell(
                    message.Quantity,
                    message.Price,
                    message.SequenceNumber,
                    message.CreatedAt);

                if (shouldDelete)
                    _repository.RemovePosition(position);
                else
                    _repository.UpdatePosition(position);
            }

            await _context.SaveChangesAsync(context.CancellationToken);

            await dbTransaction.CommitAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(context.CancellationToken);

            _logger.LogError(ex, "Error processing TransactionUpdatedEvent");

            throw;
        }
    }
}