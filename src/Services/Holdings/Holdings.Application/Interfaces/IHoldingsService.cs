using Holdings.Application.DTOs;

namespace Holdings.Application.Interfaces;

public interface IHoldingsService
{
    // Este método devuelve la "foto actual" del portafolio del usuario
    Task<PortfolioDto> GetPortfolioAsync(Guid userId);

    // Si necesitas obtener solo una posición específica (ej: cuánto tengo de AAPL)
    //Task<PositionDto?> GetPositionByTickerAsync(Guid userId, string ticker);

    Task RebuildPositionAsync(Guid userId, string ticker)
}