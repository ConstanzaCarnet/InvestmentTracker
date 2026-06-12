using MassTransit;
using Microsoft.EntityFrameworkCore;
using Transactions.Application.Interfaces;
using Transactions.Application.DTOs;
using Transactions.Infrastructure.Data;
using Transactions.Domain.Entities;
using Transactions.Domain.Exceptions;
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

    // ─────────────────────────────────────────────────────────────
    //  VALIDATION
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Looks up an active instrument by ticker. Normalises input before querying
    /// so the database index on Ticker is used (no column-side functions).
    /// Throws DomainException (→ HTTP 400) if not found or inactive.
    /// </summary>
    private async Task<Instrument> ValidateAndGetInstrumentAsync(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new DomainException("Ticker is required.");

        var normalised = ticker.Trim().ToUpperInvariant();

        var instrument = await _context.Instruments
            .FirstOrDefaultAsync(i => i.Ticker == normalised && i.IsActive);

        if (instrument is null)
            throw new DomainException(
                $"Ticker '{normalised}' does not exist or is not available for trading. " +
                $"Check the instruments catalogue for valid tickers.");

        return instrument;
    }

    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");
    }

    // ─────────────────────────────────────────────────────────────
    //  BUY / SELL
    // ─────────────────────────────────────────────────────────────

    public Task<TransactionDto> BuyAsync(Guid userId, BuyRequest request)
    {
        ValidateUserId(userId);
        return CreateTransactionInternalAsync(
            userId,
            request.Ticker,
            request.Quantity,
            request.Price,
            request.Currency,
            request.ExchangeRate,
            DomainTransactionType.BUY);
    }

    public Task<TransactionDto> SellAsync(Guid userId, SellRequest request)
    {
        ValidateUserId(userId);
        return CreateTransactionInternalAsync(
            userId,
            request.Ticker,
            request.Quantity,
            request.Price,
            request.Currency,
            request.ExchangeRate,
            DomainTransactionType.SELL);
    }

    private async Task<TransactionDto> CreateTransactionInternalAsync(
        Guid userId,
        string ticker,
        decimal quantity,
        decimal price,
        DomainCurrencyType currency,
        decimal exchangeRate,
        DomainTransactionType type)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var instrument = await ValidateAndGetInstrumentAsync(ticker);

            long nextVersion = await _repository.GetNextVersionAsync(userId, instrument.Id);

            if (type == DomainTransactionType.SELL)
            {
                var currentPosition = await _repository.GetCurrentPositionAsync(userId, instrument.Id);
                if (currentPosition < quantity)
                    throw new DomainException(
                        $"Insufficient holdings. Available: {currentPosition} {instrument.Ticker}, " +
                        $"requested sell: {quantity}.");
            }

            var transaction = new Transaction(
                userId,
                instrument.Id,
                instrument.Ticker,   // always use the canonical ticker from DB, not user input
                quantity,
                price,
                currency,
                exchangeRate,
                instrument.ConversionRatio,
                type,
                nextVersion);

            await _repository.AddAsync(transaction);
            await _context.SaveChangesAsync();

            var integrationEvent = new TransactionCreatedEvent
            {
                Id             = transaction.Id,
                UserId         = transaction.UserId,
                InstrumentId   = transaction.InstrumentId,
                Ticker         = transaction.Ticker,
                Quantity       = transaction.Quantity,
                Price          = transaction.Price,
                Currency       = transaction.Currency.ToString(),
                ExchangeRate   = transaction.ExchangeRate,
                ConversionRatio = transaction.ConversionRatio,
                Type           = MapToEventType(transaction.Type),
                CreatedAt      = transaction.CreatedAt,
                SequenceNumber = transaction.Version
            };

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id             = Guid.NewGuid(),
                Type           = integrationEvent.GetType().Name,
                Content        = JsonSerializer.Serialize(integrationEvent),
                OccurredOnUtc  = DateTime.UtcNow,
                IdempotencyKey = transaction.Id
            });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation(
                "Transaction created: {Type} {Qty} {Ticker} for user {UserId}",
                type, quantity, instrument.Ticker, userId);

            return MapToDto(transaction);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> UpdateTransactionAsync(Guid userId, Guid id, TransactionRequest request)
    {
        ValidateUserId(userId);

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var transaction = await _repository.GetByIdAsync(id);
            // Ownership: treat "not yours" the same as "not found" so we don't reveal
            // that another user's transaction exists.
            if (transaction is null || transaction.UserId != userId)
                return (false, "Transaction not found.");

            // Validate the ticker — reject unknown instruments even on update.
            var instrument = await ValidateAndGetInstrumentAsync(request.Ticker);

            var oldQuantity        = transaction.Quantity;
            var oldTicker          = transaction.Ticker;
            var oldPrice           = transaction.Price;
            var oldConversionRatio = transaction.ConversionRatio;
            var oldExchangeRate    = transaction.ExchangeRate;
            var oldInstrumentId    = transaction.InstrumentId;
            var oldType            = transaction.Type;
            var oldCurrency        = transaction.Currency.ToString();

            // Apply changes — use canonical instrument data from DB.
            transaction.InstrumentId    = instrument.Id;
            transaction.Ticker          = instrument.Ticker;
            transaction.ConversionRatio = instrument.ConversionRatio;
            transaction.Quantity        = request.Quantity;
            transaction.Price           = request.Price;
            transaction.ExchangeRate    = request.ExchangeRate;
            transaction.Currency        = request.Currency;
            transaction.CreatedAt       = request.Date;
            transaction.Type            = (DomainTransactionType)request.Type;
            transaction.LastModified    = DateTime.UtcNow;
            transaction.Version        += 1;

            _repository.Update(transaction);
            await _context.SaveChangesAsync();

            var @event = new TransactionUpdatedEvent
            {
                Id                    = transaction.Id,
                UserId                = transaction.UserId,
                InstrumentId          = transaction.InstrumentId,
                Ticker                = transaction.Ticker,
                Quantity              = transaction.Quantity,
                Price                 = transaction.Price,
                Currency              = transaction.Currency.ToString(),
                ExchangeRate          = transaction.ExchangeRate,
                ConversionRatio       = transaction.ConversionRatio,
                Type                  = MapToEventType(transaction.Type),
                CreatedAt             = DateTime.UtcNow,
                SequenceNumber        = transaction.Version,

                PreviousTicker        = oldTicker,
                PreviousQuantity      = oldQuantity,
                PreviousPrice         = oldPrice,
                PreviousExchangeRate  = oldExchangeRate,
                PreviousConversionRatio = oldConversionRatio,
                PreviousInstrumentId  = oldInstrumentId,
                PreviousCurrency      = oldCurrency,
                PreviousType          = MapToEventType(oldType),
            };

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Type          = @event.GetType().Name,
                Content       = JsonSerializer.Serialize(@event),
                OccurredOnUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return (true, "Transaction updated successfully.");
        }
        catch (DomainException)
        {
            await dbTransaction.RollbackAsync();
            throw; // re-throw so the middleware maps it to 400
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error updating transaction {Id}", id);
            return (false, "Internal error while updating transaction.");
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  DELETE
    // ─────────────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> DeleteTransactionAsync(Guid userId, Guid id)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var transaction = await _repository.GetByIdAsync(id);
            // Ownership: treat "not yours" the same as "not found".
            if (transaction is null || transaction.UserId != userId)
                return (false, "Transaction not found.");

            _repository.Remove(transaction);
            await _context.SaveChangesAsync();

            var @event = new TransactionDeletedEvent
            {
                Id              = transaction.Id,
                UserId          = transaction.UserId,
                InstrumentId    = transaction.InstrumentId,
                Ticker          = transaction.Ticker,
                Quantity        = transaction.Quantity,
                Price           = transaction.Price,
                Currency        = transaction.Currency.ToString(),
                ExchangeRate    = transaction.ExchangeRate,
                ConversionRatio = transaction.ConversionRatio,
                Type            = MapToEventType(transaction.Type),
                CreatedAt       = DateTime.UtcNow,
                SequenceNumber  = transaction.Version + 1
            };

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Type          = @event.GetType().Name,
                Content       = JsonSerializer.Serialize(@event),
                OccurredOnUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return (true, "Transaction deleted successfully.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting transaction {Id}", id);
            return (false, "Internal error while deleting transaction.");
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  QUERIES
    // ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<TransactionDto>> GetTransactionHistoryAsync(Guid userId)
        => (await _repository.GetByUserIdAsync(userId)).Select(MapToDto);

    public async Task<IEnumerable<TransactionDto>> GetTransactionHistoryByTickerAsync(Guid userId, Guid instrumentId)
        => (await _repository.GetByUserIdAndTickerAsync(userId, instrumentId)).Select(MapToDto);

    public async Task<IEnumerable<TransactionDto>> GetTransactionHistoryByDateAsync(Guid userId, DateTime date)
        => (await _repository.GetByUserIdAndDateAsync(userId, date)).Select(MapToDto);

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────

    private static TransactionDto MapToDto(Transaction t) => new(
        t.Id,
        t.Ticker,
        t.Quantity,
        t.Price,
        t.ExchangeRate,
        t.ConversionRatio,
        t.Currency.ToString(),
        t.CreatedAt,
        t.Type.ToString());

    private static EventTransactionType MapToEventType(DomainTransactionType type) => type switch
    {
        DomainTransactionType.BUY  => EventTransactionType.BUY,
        DomainTransactionType.SELL => EventTransactionType.SELL,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
