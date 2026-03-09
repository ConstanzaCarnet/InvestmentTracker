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
        // CancellationToken se propaga desde MassTransit y se puede usar para cancelar operaciones si el consumidor es detenido o si el mensaje es descartado por alguna razón (como un error de deserialización o un filtro de mensajes). Es importante respetar este token en operaciones asíncronas para permitir una cancelación adecuada y evitar que el consumidor procese mensajes innecesariamente.
        //evita que el evento se procese y que la DB quede en un estado inconsistente, si el consumidor es detenido o si el mensaje es descartado por alguna razón (como un error de deserialización o un filtro de mensajes)
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var ticker = message.Ticker.Trim().ToUpperInvariant();
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
                    _logger.LogWarning("Sell without position {Ticker}", message.UserId, ticker);
                    return;
                }

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
            //usamos context.CancellationToken para que si el consumidor es detenido o si el mensaje es descartado por alguna razón, se cancele la operación de guardado y se evite que la DB quede en un estado inconsistente
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