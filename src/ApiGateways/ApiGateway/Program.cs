using System.Security.Claims;
using System.Threading.RateLimiting;
using Common.Authentication;

var builder = WebApplication.CreateBuilder(args);

// (1) Registra el motor de YARP y le dice que lea sus rutas/clusters
//     desde la sección "ReverseProxy" de appsettings.json.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// (2) Validación de JWT con el MISMO helper que Holdings/Transactions
//     (misma Key/Issuer/Audience). Internamente también llama a AddAuthorization(),
//     que deja lista la política "default" = requiere usuario autenticado.
builder.Services.AddJwtAuthentication(builder.Configuration);

// (3) Rate limiting. Una política "per-user" que las rutas referencian por nombre
//     (igual que AuthorizationPolicy). Algoritmo: fixed window (100 req / minuto).
//     La CLAVE de partición decide a quién se le cuenta:
//       - autenticado -> por userId (claim sub/NameIdentifier)
//       - anónimo      -> por IP de origen
//     Cada clave tiene su propio contador independiente.
builder.Services.AddRateLimiter(options =>
{
    // Por defecto rechaza con 503; queremos el 429 estándar de "Too Many Requests".
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("per-user", httpContext =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = userId is not null
            ? $"user:{userId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,                 // requests permitidas...
                Window = TimeSpan.FromMinutes(1),  // ...por ventana de 1 minuto
                QueueLimit = 0                     // sin cola: lo que excede se rechaza ya
            });
    });
});

var app = builder.Build();

// Endpoint propio del gateway (NO se reenvía a nadie): chequeo rápido de vida.
app.MapGet("/", () => "API Gateway up");

// Orden del pipeline (importa): autenticar -> autorizar -> rate limit -> proxy.
// Auth va ANTES del rate limiter para que la clave "per-user" ya tenga el userId.
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Enchufa YARP. Cada Route aplica su AuthorizationPolicy/RateLimiterPolicy
// (si las declara) antes de reenviar al Cluster.
app.MapReverseProxy();

app.Run();
