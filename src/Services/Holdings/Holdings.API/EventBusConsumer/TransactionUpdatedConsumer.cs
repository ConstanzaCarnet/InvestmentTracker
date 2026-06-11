using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
using Holdings.Infrastructure.Data;
using Holdings.Domain.Enums;
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

        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var newTicker = message.Ticker.Trim().ToUpperInvariant();
            var oldTicker = message.PreviousTicker.Trim().ToUpperInvariant();

            var instrumentChanged = message.InstrumentId != message.PreviousInstrumentId;

            //  1. REVERTIR
            var oldPosition = await _repository.GetPositionAsync(
                message.UserId,
                message.PreviousInstrumentId);

            if (oldPosition != null)
            {
                var oldLot = oldPosition.GetOrCreateLot(message.PreviousCurrency);

                oldLot.Revert(
                    message.PreviousQuantity,
                    message.PreviousPrice,
                    message.PreviousConversionRatio,
                    message.PreviousExchangeRate,
                    TransactionTypeMapper.ToDomain(message.PreviousType)
                );

                if (!oldPosition.Lots.Any())
                    _repository.RemovePosition(oldPosition);
                else
                    _repository.UpdatePosition(oldPosition);
            }

            //  2. APLICAR NUEVO
            var targetInstrumentId = instrumentChanged
                ? message.InstrumentId
                : message.PreviousInstrumentId;

            var position = await _repository.GetPositionAsync(
                message.UserId,
                targetInstrumentId);

            var isNew = position == null;

            if (isNew)
            {
                position = new Position(message.UserId, targetInstrumentId, newTicker);
                await _repository.AddPositionAsync(position);
            }

            //  VALIDACIÓN GLOBAL (una sola vez)
            position!.ValidateAndApplySequence(message.SequenceNumber, message.CreatedAt);

            var lot = position.GetOrCreateLot(message.Currency);

            if (message.Type == EventBus.Messages.Events.TransactionType.BUY)
            {
                lot.Buy(message.Quantity, message.Price, message.ConversionRatio, message.ExchangeRate);
            }
            else
            {
                var shouldDeleteLot = lot.Sell(message.Quantity, message.Price, message.ConversionRatio);
                if (shouldDeleteLot)
                    position.RemoveLot(lot);
            }

            if (!position.Lots.Any())
                _repository.RemovePosition(position);
            else if (!isNew)
                _repository.UpdatePosition(position);

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