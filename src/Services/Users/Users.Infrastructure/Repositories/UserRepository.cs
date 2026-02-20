using Microsoft.EntityFrameworkCore;
using Users.Application.Interfaces;
using Users.Domain.Entities;
using Users.Infrastructure.Data;

namespace Users.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    //inyectamos el DbContext a traves del constructor para que se pueda manejar la base de datos
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _context.Users.ToListAsync();

    public async Task AddAsync(User user) =>
        await _context.Users.AddAsync(user);
    //solo es un cambio de estado, no necesitamos un await, eso lo hara el SaveChangesAsync
    public Task UpdateAsync(User user)
    {
        _context.Entry(user).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;

    public Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }
}