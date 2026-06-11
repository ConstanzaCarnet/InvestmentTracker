using MarketData.Application.Interfaces;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketData.Infrastructure.Repositories;

public class InstrumentRepository : IInstrumentRepository
{
    private readonly MarketDataContext _context;

    public InstrumentRepository(MarketDataContext context)
    {
        _context = context;
    }

    public async Task<List<Instrument>> GetByIdsAsync(List<Guid> ids)
    {
        return await _context.Instruments
            .Where(i => ids.Contains(i.Id))
            .ToListAsync();
    }
}
