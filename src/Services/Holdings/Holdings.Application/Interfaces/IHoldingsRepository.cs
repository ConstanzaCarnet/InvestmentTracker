using Holdings.Domain.Entities;

namespace Holdings.Application.Interfaces;

public interface IHoldingRepository
{
    Task<Account?> GetAccountByUserIdAsync(Guid userId);
    Task CreateAccountAsync(Account account);
    Task UpdateAccountAsync(Account account);
}