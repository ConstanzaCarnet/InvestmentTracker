using System.Security.Cryptography;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Domain.Entities;
using Users.Domain.Exceptions;
using MassTransit;
using EventBus.Messages.Events;

namespace Users.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public UserService(
        IUserRepository repository,
        IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> CreateUserAsync(UserCreateDto dto)
    {
        var existingUser = await _repository.GetByEmailAsync(dto.Email);

        if (existingUser != null)
            throw new DomainException("Email already registered.", statusCode: 409);

        var passwordHash = HashPassword(dto.Password);

        var user = new User(dto.Email, dto.FirstName, dto.LastName, passwordHash);

        await _repository.AddAsync(user);
        await _repository.SaveChangesAsync();

        await _publishEndpoint.Publish(new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        });

        return user.Id;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _repository.GetAllAsync();

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName
        });
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);

        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }

    public async Task UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var user = await _repository.GetByIdAsync(id);

        if (user == null)
            throw new DomainException("User not found.", statusCode: 404);

        user.UpdateInfo(dto.FirstName, dto.LastName);
        user.UpdatePassword(HashPassword(dto.Password));

        await _repository.UpdateAsync(user);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);

        if (user == null)
            throw new DomainException("User not found.", statusCode: 404);

        await _repository.DeleteAsync(user);
        await _repository.SaveChangesAsync();

        await _publishEndpoint.Publish(new UserDeletedEvent
        {
            UserId = user.Id,
            Email = user.Email
        });
    }

    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations: 350_000,
            HashAlgorithmName.SHA512, outputLength: 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}