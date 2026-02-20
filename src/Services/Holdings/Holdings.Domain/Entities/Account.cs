namespace Holdings.Domain.Entities;

public class Account
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; } // Referencia al usuario de Users.API
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public DateTime LastUpdated { get; set; }
}