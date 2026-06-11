using Holdings.Application.DTOs;

namespace Holdings.Application.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioDto> GetPortfolioAsync(Guid userId);
}