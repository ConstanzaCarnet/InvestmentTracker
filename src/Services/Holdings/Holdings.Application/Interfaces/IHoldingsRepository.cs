using Holdings.Domain.Entities;

namespace Holdings.Application.Interfaces;

public interface IHoldingRepository
{
    Task<Account?> GetByIdAsync(Guid userId);
    Task<Position?> GetPositionAsync(Guid userId, string ticker);
    Task AddPositionAsync(Position position);
    void UpdatePosition(Position position);
    void RemovePosition(Position position);
}
