using Holdings.Domain.Entities;
using Holdings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Holdings.Infrastructure.Data;

namespace Holdings.Infrastructure.Repositories;

public class HoldingRepository : IHoldingRepository
{
    private readonly HoldingsDbContext _context;

    public HoldingRepository(HoldingsDbContext context) => _context = context;

    public async Task<Account?> GetAccountByUserIdAsync(Guid userId)
    {
        return await _context.Accounts
            .Include(a => a.Positions)//importante para cargar las posiciones relacionadas con la cuenta y que no se tenga que hacer una consulta adicional para obtenerlas posteriormente
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task CreateAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAccountAsync(Account account)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }
}