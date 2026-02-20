using Users.Domain.Common;

namespace Users.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // El "Hash" es la contraseña encriptada.
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsKycVerified { get; set; } = false;

    public User(string email, string firstName, string lastName, string passwordHash)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
    }
    
    public void UpdatePassword(string newHash)
    {
        PasswordHash = newHash;
        LastModifiedDate = DateTime.UtcNow;
    }

    //Metodo de negosio (Domain Logic)
    public void VerifyUser()
    {
        IsKycVerified = true;
        LastModifiedDate = DateTime.UtcNow;
    }

    public void UpdateInfo(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        LastModifiedDate = DateTime.UtcNow;
    }
}