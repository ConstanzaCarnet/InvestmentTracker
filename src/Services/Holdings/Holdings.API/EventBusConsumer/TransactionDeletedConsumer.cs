using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
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
            var ticker = message.Ticker.ToUpper();

            var position = await _repository.GetPositionAsync(message.UserId, ticker);

            if (position == null)
            {
                _logger.LogWarning(
                    "Delete event but no position found User={User} Ticker={Ticker}",
                    message.UserId, ticker);
                return;
            }

            // revertimos el efecto de la transacción
            position.Revert(
                message.Quantity,
                message.Price,
                message.Type);

            if (position.Quantity == 0)
                _repository.RemovePosition(position);
            else
                _repository.UpdatePosition(position);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}