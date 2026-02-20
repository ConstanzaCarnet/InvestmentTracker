using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
using MassTransit;

namespace Holdings.API.EventBusConsumer;

public class TransactionDeletedConsumer : IConsumer<TransactionDeletedEvent>
{
    private readonly IHoldingRepository _repository;
    private readonly ILogger<TransactionDeletedConsumer> _logger;

    public TransactionDeletedConsumer(IHoldingRepository repository,ILogger<TransactionDeletedConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionDeletedEvent> context)
    {
        var message = context.Message;
        var account = await _repository.GetAccountByUserIdAsync(message.UserId);

        if (account == null)
        {
            throw new InvalidOperationException($"Cuenta no encontrada para UserId {message.UserId}. Reintentando...");
        }

        var position = account.Positions.FirstOrDefault(p => p.Ticker == message.Ticker);
        if (position == null)
        {
            throw new InvalidOperationException($"Posición para {message.Ticker} no encontrada. Reintentando...");
        }
        // Ignoramos mensajes antiguos
        if (message.CreatedAt < position.LastTransactionDate)
        {
            _logger.LogWarning(
                "Eliminación ignorada por mensaje antiguo. Ticker={Ticker}, MsgDate={MsgDate}, LastDate={LastDate}",
                message.Ticker, message.CreatedAt, position.LastTransactionDate);
            return;
        }
        // Si borramos una COMPRA, restamos cantidad del holding
        if (message.Type == TransactionType.BUY)
        {
            position.Quantity -= message.Quantity;

            // Si la posición queda en 0 o menos por algún error de borrado, la quitamos
            if (position.Quantity <= 0) account.Positions.Remove(position);
        }
        // Si borramos una VENTA, devolvemos la cantidad al holding
        else if (message.Type == TransactionType.SELL)
        {
            position.Quantity += message.Quantity;
        }

        await _repository.UpdateAccountAsync(account);
        _logger.LogInformation("Eliminación procesada con éxito para {Ticker}.", message.Ticker);
    }
}