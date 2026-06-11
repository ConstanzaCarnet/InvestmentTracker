using Users.Domain.Entities;

namespace Users.Application.Interfaces;

// El "qué": dado un usuario, producir un token firmado.
// El "cómo" (librería, algoritmo, clave) vive en Infrastructure.
public interface IJwtTokenGenerator
{
    GeneratedToken GenerateToken(User user);
}

// El generador conoce la expiración (la calcula con la config), así que la devuelve.
public record GeneratedToken(string AccessToken, DateTime ExpiresAtUtc);
