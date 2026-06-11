namespace Users.Infrastructure.Authentication;

// POCO que mapea la sección "Jwt" de appsettings.json / variables de entorno.
// Se llena con services.Configure<JwtSettings>(config.GetSection("Jwt")).
public class JwtSettings
{
    public const string SectionName = "Jwt";

    // Clave secreta para firmar (HMAC-SHA256 necesita >= 32 bytes / 256 bits).
    // NUNCA va en el repo en producción: en prod viene de un secret manager.
    public string Key { get; set; } = string.Empty;

    // Quién emite el token y para quién es válido. El validador los comprueba.
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    // Cuánto vive el token. Corto = más seguro (menos ventana si se roba).
    public int ExpiryMinutes { get; set; } = 60;
}
