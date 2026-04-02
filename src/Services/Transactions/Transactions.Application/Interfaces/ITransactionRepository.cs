using Transactions.Domain.Entities;

namespace Transactions.Application.Interfaces;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId);
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<Transaction>> GetByUserIdAndTickerAsync(Guid userId, Guid instrumentId);
    Task<IEnumerable<Transaction>> GetByUserIdAndDateAsync(Guid userId, DateTime date);
    Task AddAsync(Transaction transaction);
    //estos metodos devuelven void porque no es necesario esperar a que se complete la operación de actualización o eliminación, ya que estas operaciones suelen ser rápidas y no requieren una respuesta inmediata. Además, al devolver void, se simplifica la interfaz del repositorio y se evita la necesidad de manejar tareas asíncronas para estas operaciones, lo que puede mejorar la legibilidad del código y reducir la complejidad en casos donde no se necesita un resultado específico después de la actualización o eliminación.
    //eso lo hacemos con SaveChangesAsync() en el UnitOfWork, que es el encargado de guardar los cambios en la base de datos, y ahí si es necesario esperar a que se complete la operación de guardado, pero no es necesario hacerlo en cada operación de actualización o eliminación.
    void Update(Transaction transaction);
    void Remove(Transaction transaction);
    Task<long> GetNextVersionAsync(Guid userId, Guid instrumentId);
    Task<decimal> GetCurrentPositionAsync(Guid userId, Guid instrumentId);
}