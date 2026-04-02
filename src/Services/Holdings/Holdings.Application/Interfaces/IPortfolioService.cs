using Holdings.Application.DTOs;

namespace Holdings.Domain.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioDto> GetPortfolioAsync(Guid userId);
}