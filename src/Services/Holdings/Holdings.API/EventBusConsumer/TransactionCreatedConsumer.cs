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

        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var ticker = message.Ticker.ToUpper();
            //obtenemos la posición actual del usuario para ese ticker, si existe
            var position = await _repository.GetPositionAsync(message.UserId, ticker);
            //la idempotencia se verifica dentro de los métodos Buy y Sell de la entidad Position, que lanzarán una excepción si el número de secuencia ya ha sido procesado
            if (message.Type == TransactionType.BUY)
            {
                if (position == null)
                {
                    position = new Position(message.UserId, ticker);

                    position.Buy(
                        message.Quantity,
                        message.Price,
                        message.SequenceNumber,
                        message.CreatedAt);

                    await _repository.AddPositionAsync(position);
                }
                else
                {
                    position.Buy(
                        message.Quantity,
                        message.Price,
                        message.SequenceNumber,
                        message.CreatedAt);

                    _repository.UpdatePosition(position);
                }
            }
            else // SELL
            {
                if (position == null)
                {
                    _logger.LogWarning("Sell without position {Ticker}", ticker);
                    return;
                }

                var shouldDelete = position.Sell(
                    message.Quantity,
                    message.SequenceNumber,
                    message.CreatedAt);

                if (shouldDelete)
                    _repository.RemovePosition(position);
                else
                    _repository.UpdatePosition(position);
            }

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