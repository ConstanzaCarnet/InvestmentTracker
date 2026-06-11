using MarketData.Domain.Entities;

namespace MarketData.Application.Interfaces;

public interface IInstrumentRepository
{
    Task<List<Instrument>> GetByIdsAsync(List<Guid> ids);
}
