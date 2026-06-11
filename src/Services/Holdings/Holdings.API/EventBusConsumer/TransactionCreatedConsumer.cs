using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
using Holdings.Infrastructure.Data;
using MassTransit;

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

        _logger.LogInformation(
            "Processing TransactionCreated: UserId={UserId} Ticker={Ticker} Type={Type} Qty={Qty} Seq={Seq}",
            message.UserId, message.Ticker, message.Type, message.Quantity, message.SequenceNumber);

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var ticker = message.Ticker.Trim().ToUpperInvariant();

            var position = await _repository.GetPositionAsync(message.UserId, message.InstrumentId);
            var isNew = position == null;

            if (message.Type == TransactionType.BUY)
            {
                if (isNew)
                {
                    position = new Position(message.UserId, message.InstrumentId, ticker);
                    await _repository.AddPositionAsync(position);
                }

                position!.ValidateAndApplySequence(message.SequenceNumber, message.CreatedAt);

                var lot = position.GetOrCreateLot(message.Currency);
                lot.Buy(message.Quantity, message.Price, message.ConversionRatio, message.ExchangeRate);

                // Solo llamar Update para posiciones existentes.
                // Para posiciones nuevas, EF ya las trackea como Added desde AddPositionAsync.
                if (!isNew)
                    _repository.UpdatePosition(position);
            }
            else // SELL
            {
                if (position == null)
                {
                    _logger.LogWarning(
                        "Sell without position. UserId={UserId} Ticker={Ticker}", message.UserId, ticker);
                    return;
                }

                position.ValidateAndApplySequence(message.SequenceNumber, message.CreatedAt);

                var lot = position.GetOrCreateLot(message.Currency);
                var shouldDeleteLot = lot.Sell(message.Quantity, message.Price, message.ConversionRatio);

                if (shouldDeleteLot)
                    position.RemoveLot(lot);

                if (!position.Lots.Any())
                    _repository.RemovePosition(position);
                else
                    _repository.UpdatePosition(position);
            }

            await _context.SaveChangesAsync(context.CancellationToken);
            await dbTransaction.CommitAsync(context.CancellationToken);

            _logger.LogInformation(
                "TransactionCreated processed OK: UserId={UserId} Ticker={Ticker}", message.UserId, ticker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process TransactionCreated: UserId={UserId} Ticker={Ticker} Seq={Seq}",
                message.UserId, message.Ticker, message.SequenceNumber);
            await dbTransaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }
}
