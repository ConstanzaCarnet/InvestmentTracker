using Microsoft.EntityFrameworkCore;
using Transactions.Application.Interfaces;
using Transactions.Infrastructure.Data;
using Transactions.Domain.Entities;
using Transactions.Domain.Enums;

namespace Transactions.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _context;

    public TransactionRepository(TransactionDbContext context) => _context = context;

    public async Task AddAsync(Transaction transaction)
        =>await _context.Transactions.AddAsync(transaction);

    public async Task<Transaction?> GetByIdAsync(Guid id)
        => await _context.Transactions.FindAsync(id);

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId)
        =>await _context.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();

    //agrgamos busqueda por triket
    public async Task<IEnumerable<Transaction>> GetByUserIdAndTickerAsync(Guid userId, Guid instrumentId)
        => await _context.Transactions
            .Where(t => t.UserId == userId && t.InstrumentId == instrumentId)
            .ToListAsync();
    //busqueda por Id y fecha
    public async Task<IEnumerable<Transaction>> GetByUserIdAndDateAsync(Guid userId, DateTime date)
        => await _context.Transactions
            .Where(t => t.UserId == userId && t.CreatedAt.Date == date.Date)
            .ToListAsync();

    public void Update(Transaction transaction)
        => _context.Transactions.Update(transaction);

    public void Remove(Transaction transaction)
        => _context.Transactions.Remove(transaction);

    public async Task<long> GetNextVersionAsync(Guid userId, Guid instrumentId)
    {
        var lastVersion = await _context.Transactions
            .Where(t => t.UserId == userId && t.InstrumentId == instrumentId)//busca la ultima version
            .MaxAsync(t => (long?)t.Version);
        //retorna la siguiente o si no existe 0
        return (lastVersion ?? 0) + 1;
    }

    public async Task<decimal> GetCurrentPositionAsync(Guid userId, Guid instrumentId)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId && t.InstrumentId == instrumentId)
            .SumAsync(t =>
                t.Type == TransactionType.BUY
                ? t.Quantity
                : -t.Quantity);
    }

}