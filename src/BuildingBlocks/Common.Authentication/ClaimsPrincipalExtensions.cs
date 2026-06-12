using System.Security.Claims;

namespace Common.Authentication;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extrae el userId del token autenticado. JwtBearer remapea el claim "sub"
    /// a ClaimTypes.NameIdentifier; chequeamos ambos por las dudas.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub")
                    ?? throw new InvalidOperationException("El token no contiene el claim de userId (sub).");

        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("El claim de userId no es un Guid válido.");
    }
}
