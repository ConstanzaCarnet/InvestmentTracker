using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Users.Application.Interfaces;
using Users.Domain.Entities;

namespace Users.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    // IOptions<JwtSettings> = la config "Jwt" ya bindeada por el contenedor DI.
    public JwtTokenGenerator(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public GeneratedToken GenerateToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.ExpiryMinutes);

        // --- 1. CLAIMS: la "carga útil" del token (lo que afirma sobre el usuario).
        // Usamos nombres estándar (sub, email, jti) para que cualquier servicio los entienda.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),   // subject = userId
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // id único del token
            new Claim("name", $"{user.FirstName} {user.LastName}".Trim()),
        };

        // --- 2. FIRMA: clave simétrica + HMAC-SHA256.
        // El mismo secreto se usa para firmar (aquí) y para validar (en Program.cs).
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // --- 3. ARMAR Y SERIALIZAR el token a su forma "header.payload.signature".
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);
        return new GeneratedToken(serialized, expires);
    }
}
