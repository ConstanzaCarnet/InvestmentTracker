using Transactions.Application.DTOs;
using EventBus.Messages.Events;

namespace Transactions.Application.Interfaces;

public interface ITransactionService
{
    Task<TransactionDto> BuyAsync(Guid userId, BuyRequest request);
    Task<TransactionDto> SellAsync(Guid userId, SellRequest request);
    Task<IEnumerable<TransactionDto>> GetTransactionHistoryAsync(Guid userId);
    Task<IEnumerable<TransactionDto>> GetTransactionHistoryByTickerAsync(Guid userId, Guid instrumentId);
    Task<IEnumerable<TransactionDto>> GetTransactionHistoryByDateAsync(Guid userId, DateTime date);
    Task<(bool Success, string Message)> UpdateTransactionAsync(Guid id, TransactionRequest request);
    Task<(bool Success, string Message)> DeleteTransactionAsync(Guid id);
}