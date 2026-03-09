using MassTransit;
using Microsoft.EntityFrameworkCore;
using Transactions.Application.Interfaces;
using Transactions.Application.DTOs;
using Transactions.Infrastructure.Data;
using Transactions.Domain.Entities;
using EventBus.Messages.Events;
using Transactions.Domain.Enums;
using System.Text.Json;
using Transactions.Infrastructure.Outbox;

using DomainTransactionType = Transactions.Domain.Enums.TransactionType;
using DomainCurrencyType = Transactions.Domain.Enums.Currency;
using EventTransactionType = EventBus.Messages.Events.TransactionType;

namespace Transactions.API.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly TransactionDbContext _context;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionRepository repository,
        TransactionDbContext context,
        ILogger<TransactionService> logger)
    {
        _repository = repository;
        _context = context;
        _logger = logger;
    }

    private static EventBus.Messages.Events.TransactionType MapToEventType(
    Transactions.Domain.Enums.TransactionType type)
    {
        return type switch
        {
            Transactions.Domain.Enums.TransactionType.BUY =>
                EventBus.Messages.Events.TransactionType.BUY,

            Transactions.Domain.Enums.TransactionType.SELL =>
                EventBus.Messages.Events.TransactionType.SELL,

            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    public async Task<TransactionDto> BuyAsync(BuyRequest request)
    {
        return await CreateTransactionInternalAsync(
            request.UserId,
            request.Ticker,
            request.Quantity,
            request.Price,
            request.Currency,
            request.ExchangeRate,
            DomainTransactionType.BUY);
    }

    public async Task<TransactionDto> SellAsync(SellRequest request)
    {
        return await CreateTransactionInternalAsync(
            request.UserId,
            request.Ticker,
            request.Quantity,
            request.Price,
            request.Currency,
            request.ExchangeRate,
            DomainTransactionType.SELL);
    }

    private async Task<TransactionDto> CreateTransactionInternalAsync(Guid userId, string ticker, decimal quantity, decimal price, DomainCurrencyType currency, decimal exchangeRate, DomainTransactionType type)
    {
        // inicio una transacción de base de datos para asegurar atomicidad entre la creación de la transacción y el evento de integración
        //no es lo mismo que _context.Database.BeginTransaction() que es sincrono, este es asincrono y se adapta mejor a la naturaleza async de nuestro servicio
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1️. obtener la próxima versión (orden del ledger)
            long nextVersion = await _repository.GetNextVersionAsync(userId, ticker);
            //reviso si es posible crear la transaction o si el balance total no lo permite
            if (type == DomainTransactionType.SELL)
            {
                var currentPosition = await _repository.GetCurrentPositionAsync(userId, ticker);

                if (currentPosition < quantity)
                    throw new InvalidOperationException(
                        $"No tienes suficiente {ticker} para vender. Disponible: {currentPosition}");
            }
            // 2️. crear la entidad
            var transaction = new Transaction(
                userId,
                ticker.ToUpper(),
                quantity,
                price,
                currency,
                exchangeRate,
                type,
                nextVersion
            );
            // 3️. guardar
            await _repository.AddAsync(transaction);
            await _context.SaveChangesAsync();


            // 4. crea el evento de integración, para luego publicarlo mediante el outbox pattern
            var integrationEvent = new TransactionCreatedEvent
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                Ticker = transaction.Ticker,
                Quantity = transaction.Quantity,
                Price = transaction.Price,
                Currency = transaction.Currency.ToString(),
                ExchangeRate = transaction.ExchangeRate,
                Type = MapToEventType(transaction.Type),
                CreatedAt = transaction.CreatedAt,
                SequenceNumber = transaction.Version
            };
            var outboxMessage = new OutboxMessage
            {
                Type = integrationEvent.GetType().Name,
                Content = JsonSerializer.Serialize(integrationEvent),
                OccurredOnUtc = DateTime.UtcNow
            };
            // 5.Guardar → Guardar evento → Commit
            _context.OutboxMessages.Add(outboxMessage);
            await _context.SaveChangesAsync();

            // el commit guarda: 
            // 1) la transacción, es decir, el nuevo estado del ledger
            // 2) el evento, que luego será publicado por el proceso de outbox
            await dbTransaction.CommitAsync();

            // 6. mapear a DTO y retornar
            return new TransactionDto(
                transaction.Id,
                transaction.Ticker,
                transaction.Quantity,
                transaction.Price,
                transaction.ExchangeRate,
                transaction.Currency.ToString(),
                transaction.CreatedAt,
                transaction.Type.ToString()
            );
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error creando transacción");
            throw;
        }
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionHistoryAsync(Guid userId)
    {
        var transactions = await _repository.GetByUserIdAsync(userId);

        return transactions.Select(t => new TransactionDto(
            t.Id,
            t.Ticker,
            t.Quantity,
            t.Price,
            t.ExchangeRate,
            t.Currency.ToString(),
            t.CreatedAt,
            t.Type.ToString()
        ));
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionHistoryByTickerAsync(Guid userId, string ticker)
    {
        var transactions = await _repository.GetByUserIdAndTickerAsync(userId, ticker.ToUpper());
        return transactions.Select(t => MapToDto(t));
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionHistoryByDateAsync(Guid userId, DateTime date)
    {
        var transactions = await _repository.GetByUserIdAndDateAsync(userId, date);
        return transactions.Select(t => MapToDto(t));
    }

    private static TransactionDto MapToDto(Transaction t)
    {
        return new TransactionDto(
            t.Id,
            t.Ticker,
            t.Quantity,
            t.Price,
            t.ExchangeRate,
            t.Currency.ToString(),
            t.CreatedAt,
            t.Type.ToString()
        );
    }
    //actualizar
    public async Task<(bool Success, string Message)> UpdateTransactionAsync(Guid id, TransactionRequest request)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        var transaction = await _repository.GetByIdAsync(id);

        if (transaction == null)
            return (false, "Transacción no encontrada");

        var oldQuantity = transaction.Quantity;
        var oldTicker = transaction.Ticker;
        var oldPrice = transaction.Price;
        var oldType = transaction.Type;

        // aplicar cambios
        transaction.Quantity = request.Quantity;
        transaction.Price = request.Price;
        transaction.CreatedAt = request.Date;
        transaction.Type = (DomainTransactionType)request.Type;
        transaction.LastModified = DateTime.UtcNow;

        // ⚠ importante
        transaction.Version += 1;

        try
        {
            _repository.Update(transaction);
            await _context.SaveChangesAsync();

            var @event = new TransactionUpdatedEvent
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                Ticker = transaction.Ticker,
                Quantity = transaction.Quantity,
                Price = transaction.Price,
                Type = MapToEventType(transaction.Type),
                CreatedAt = DateTime.UtcNow,

                PreviousTicker = oldTicker,
                PreviousQuantity = oldQuantity,
                PreviousPrice = oldPrice,
                PreviousCurrency = transaction.Currency.ToString(),
                PreviousType = MapToEventType(oldType),

                SequenceNumber = transaction.Version
            };

            var outboxMessage = new OutboxMessage
            {
                Type = @event.GetType().Name,
                Content = JsonSerializer.Serialize(@event),
                OccurredOnUtc = DateTime.UtcNow
            };

            _context.OutboxMessages.Add(outboxMessage);
            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            return (true, "Transacción actualizada");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error updating transaction");
            return (false, "Error interno");
        }
    }


    public async Task<(bool Success, string Message)> DeleteTransactionAsync(Guid id)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return (false, "Transacción no encontrada");

        try
        {
            _repository.Remove(transaction);
            await _context.SaveChangesAsync();

            var @event = new TransactionDeletedEvent
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                Ticker = transaction.Ticker,
                Quantity = transaction.Quantity,
                Price = transaction.Price,
                Currency = transaction.Currency.ToString(),
                ExchangeRate = transaction.ExchangeRate,
                Type = MapToEventType(transaction.Type),
                CreatedAt = DateTime.UtcNow,
                SequenceNumber = transaction.Version + 1
            };

            var outboxMessage = new OutboxMessage
            {
                Type = @event.GetType().Name,
                Content = JsonSerializer.Serialize(@event),
                OccurredOnUtc = DateTime.UtcNow
            };

            _context.OutboxMessages.Add(outboxMessage);
            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            return (true, "Transacción eliminada");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting transaction");
            return (false, "Error interno");
        }
    }


}
