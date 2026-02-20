using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
using MassTransit;

namespace Holdings.API.EventBusConsumer;


public class TransactionCreatedConsumer : IConsumer<TransactionCreatedEvent>
{
    private readonly IHoldingRepository _repository;
    private readonly ILogger<TransactionCreatedConsumer> _logger;

    public TransactionCreatedConsumer( IHoldingRepository repository, ILogger<TransactionCreatedConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreatedEvent> context)
    {
        var message = context.Message;

        // --- VALIDACIONES BÁSICAS ---
        if (string.IsNullOrWhiteSpace(message.Ticker))
        {
            _logger.LogWarning("TransactionCreatedEvent inválido: Ticker vacío. UserId={UserId}", message.UserId);
            return;
        }

        if (message.Quantity <= 0)
        {
            _logger.LogWarning("TransactionCreatedEvent inválido: Quantity <= 0. Ticker={Ticker}", message.Ticker);
            return;
        }

        if (message.Type != TransactionType.BUY && message.Type != TransactionType.SELL)
        {
            _logger.LogWarning("TransactionCreatedEvent inválido: Type desconocido {Type}", message.Type);
            return;
        }

        if (message.Type == TransactionType.BUY && message.Price <= 0)
        {
            _logger.LogWarning("TransactionCreatedEvent inválido: Price <= 0 en compra. Ticker={Ticker}", message.Ticker);
            return;
        }

        // 1) Buscar o crear cuenta
        var account = await _repository.GetAccountByUserIdAsync(message.UserId);
        if (account == null)
        {
            account = new Account
            {
                AccountId = Guid.NewGuid(),
                UserId = message.UserId
            };

            await _repository.CreateAccountAsync(account);
        }

        // 2) Buscar posición
        var position = account.Positions.FirstOrDefault(p => p.Ticker == message.Ticker);

        // Si no existe posición y es Sell => inválido
        if (position == null && message.Type == TransactionType.SELL)
        {
            _logger.LogWarning("Venta inválida: no existe posición para {Ticker}", message.Ticker);
            return;
        }

        // ---------- CONTROL DE ORDEN E IDEMPOTENCIA ----------
        if (position != null)
        {
            // duplicado
            if (message.SequenceNumber <= position.LastProcessedSequenceNumber)
            {
                _logger.LogInformation(
                    "Evento duplicado ignorado. Ticker={Ticker}, Seq={Seq}",
                    message.Ticker, message.SequenceNumber);
                return;
            }

            // evento futuro (llegó fuera de orden)
            if (message.SequenceNumber > position.LastProcessedSequenceNumber + 1)
            {
                _logger.LogWarning(
                    "Evento fuera de orden. Esperado={Expected} Recibido={Received}",
                    position.LastProcessedSequenceNumber + 1,
                    message.SequenceNumber);

                // IMPORTANTÍSIMO:
                // Lanzamos excepción para que MassTransit reintente
                throw new Exception("Out of order event");
            }
        }

        // --- APLICAR EVENTO ---
        if (message.Type == TransactionType.BUY )
        {
            if (position == null)
            {
                position = new Position
                {
                    Ticker = message.Ticker,
                    Quantity = message.Quantity,
                    AveragePurchasePrice = message.Price,
                    LastTransactionDate = message.CreatedAt
                };

                account.Positions.Add(position);
            }
            else
            {
                // promedio ponderado
                var totalCost = (position.Quantity * position.AveragePurchasePrice) +
                                (message.Quantity * message.Price);

                position.Quantity += message.Quantity;
                position.AveragePurchasePrice = totalCost / position.Quantity;

                position.LastTransactionDate = message.CreatedAt;
            }
        }
        else // Sell
        {
            if (position!.Quantity < message.Quantity)
            {
                _logger.LogWarning("Venta inválida: cantidad insuficiente. Ticker={Ticker}", message.Ticker);
                return;
            }

            position.Quantity -= message.Quantity;
            position.LastTransactionDate = message.CreatedAt;

            if (position.Quantity == 0)
                account.Positions.Remove(position);
        }

        position.LastProcessedSequenceNumber = message.SequenceNumber;
        position.LastTransactionDate = message.CreatedAt;

        await _repository.UpdateAccountAsync(account);
    }
}