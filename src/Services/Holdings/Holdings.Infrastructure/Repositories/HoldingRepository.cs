using Holdings.Domain.Entities;
using Holdings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Holdings.Infrastructure.Data;

namespace Holdings.Infrastructure.Repositories;
//Aqui no tenemos SaveChangesAsync porque el consumer controlará la transacción de evento.
public class HoldingRepository : IHoldingRepository
{
    private readonly HoldingsDbContext _context;

    public HoldingRepository(HoldingsDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid userId)
    {
        //position debe encontrarse con el userId, no con accountId, porque queremos evitar bloqueos al actualizar posiciones, y el accountId es solo un identificador lógico que no se relaciona directamente con las posiciones
        return await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<Position?> GetPositionAsync(Guid userId, string ticker)
    {
        return await _context.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Ticker == ticker);
    }

    public async Task AddPositionAsync(Position position)
    {
        await _context.Positions.AddAsync(position);
    }

    public void UpdatePosition(Position position)
    {
        _context.Positions.Update(position);
    }

    public void RemovePosition(Position position)
    {
        _context.Positions.Remove(position);
    }
}