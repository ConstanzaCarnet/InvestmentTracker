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

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var ticker = message.Ticker.Trim().ToUpperInvariant();
            var type = TransactionTypeMapper.ToDomain(message.Type);

            var position = await _repository.GetPositionAsync(message.UserId, ticker);

            if (position == null)
            {
                _logger.LogWarning(
                    "Delete event but no position found User={User} Ticker={Ticker}",
                    message.UserId,
                    ticker);
                return;
            }

            // revertimos el efecto de la transacción
            position.Revert(
                message.Quantity,
                message.Price,
                type);

            if (position.Quantity == 0)
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
}