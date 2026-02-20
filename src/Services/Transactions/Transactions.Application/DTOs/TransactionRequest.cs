using Transactions.Domain.Entities;
using EventBus.Messages.Events;

namespace Transactions.Application.DTOs;

public record TransactionRequest
(
    Guid UserId,
    string Ticker,// simbolo de la accion
    decimal Quantity,
    decimal Price,
    DateTime Date,
    TransactionType Type // Buy o Sell
);