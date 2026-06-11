using Users.Application.DTOs;

namespace Users.Application.Interfaces;

public interface IUserService
{
    Task<Guid> CreateUserAsync(UserCreateDto dto);

    Task<AuthResponseDto> LoginAsync(LoginDto dto);

    Task<IEnumerable<UserDto>> GetAllAsync();

    Task<UserDto?> GetByIdAsync(Guid id);

    Task UpdateAsync(Guid id, UserUpdateDto dto);

    Task DeleteAsync(Guid id);
}