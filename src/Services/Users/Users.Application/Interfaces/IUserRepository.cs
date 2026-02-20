using Users.Domain.Entities;
using Users.Application.DTOs;
using Microsoft.AspNetCore.Mvc;


namespace Users.Application.Interfaces;
//esto definirá los métodos que el repositorio debe implementar, para luego llamarlos desde el controlador
//lo que facilita el testing y la mantenibilidad
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> SaveChangesAsync();
    Task DeleteAsync(User user);
}