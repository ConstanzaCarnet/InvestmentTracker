using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
using MassTransit;

namespace Holdings.API.EventBusConsumer;

public class TransactionUpdatedConsumer : IConsumer<TransactionUpdatedEvent>
{
    private readonly IHoldingRepository _repository;
    private readonly ILogger<TransactionUpdatedConsumer> _logger;

    public TransactionUpdatedConsumer(
        IHoldingRepository repository,
        ILogger<TransactionUpdatedConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionUpdatedEvent> context)
    {
        var message = context.Message;
        var account = await _repository.GetAccountByUserIdAsync(message.UserId);
        // Si la cuenta no existe, lanzamos excepción para reintentar luego
        if (account == null)
            throw new InvalidOperationException($"Cuenta no encontrada para UserId {message.UserId}. Reintentando...");

        var previousTicker = string.IsNullOrWhiteSpace(message.PreviousTicker) ? message.Ticker : message.PreviousTicker;
        var previousPosition = account.Positions.FirstOrDefault(p => p.Ticker == previousTicker);
        // Si la posición no existe, lanzamos excepción para reintentar luego
        if (previousPosition == null)
            throw new InvalidOperationException($"Posición para {previousTicker} no encontrada. Reintentando...");
        // --- PASO 1: REVERTIR EN EL TICKER ANTERIOR ---
        if (previousPosition != null)
        {
            if (message.CreatedAt < previousPosition.LastTransactionDate)
            {
                _logger.LogWarning("Mensaje ignorado: actualización antigua para {Ticker}", previousTicker);
                return;
            }

            if (message.Type == TransactionType.BUY)
            {
                // antes fue Buy => revertimos restando
                previousPosition.Quantity -= message.PreviousQuantity;
            }
            else if (message.Type == TransactionType.SELL)
            {
                // antes fue Sell => revertimos sumando
                previousPosition.Quantity += message.PreviousQuantity;
            }

            if (previousPosition.Quantity <= 0)
                account.Positions.Remove(previousPosition);
            else
                previousPosition.LastTransactionDate = message.CreatedAt;
        }

        // --- PASO 2: APLICAR EN EL TICKER NUEVO ---
        var newPosition = account.Positions.FirstOrDefault(p => p.Ticker == message.Ticker);

        if (newPosition == null)
        {
            // si el ticker cambió o si no existía, creamos posición
            newPosition = new Position
            {
                Ticker = message.Ticker,
                Quantity = 0,
                AveragePurchasePrice = message.Price, // base inicial
                LastTransactionDate = message.CreatedAt
            };

            account.Positions.Add(newPosition);
        }
        else
        {
            // Validación de orden para el ticker nuevo también
            if (message.CreatedAt < newPosition.LastTransactionDate)
            {
                _logger.LogWarning("Mensaje ignorado: actualización antigua para {Ticker}", message.Ticker);
                return;
            }
        }

        if (message.Type == TransactionType.BUY)
        {
            // Recalcular PPC (AveragePurchasePrice) al comprar
            var oldQty = newPosition.Quantity;
            var newQty = oldQty + message.Quantity;

            if (newQty > 0)
            {
                var totalCost = (oldQty * newPosition.AveragePurchasePrice) + (message.Quantity * message.Price);
                newPosition.Quantity = newQty;
                newPosition.AveragePurchasePrice = totalCost / newQty;
            }

            newPosition.LastTransactionDate = message.CreatedAt;
        }
        else if (message.Type == TransactionType.SELL)
        {
            if (newPosition.Quantity < message.Quantity)
            {
                _logger.LogWarning("Venta inválida para {Ticker}. Cantidad insuficiente.", message.Ticker);
                return;
            }

            newPosition.Quantity -= message.Quantity;

            if (newPosition.Quantity <= 0)
                account.Positions.Remove(newPosition);
            else
                newPosition.LastTransactionDate = message.CreatedAt;
        }
        else
        {
            _logger.LogWarning("Tipo de transacción inválido: {Type}", message.Type);
            return;
        }

        await _repository.UpdateAccountAsync(account);
    }
}
