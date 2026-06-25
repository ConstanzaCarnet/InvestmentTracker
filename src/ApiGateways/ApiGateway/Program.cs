using System.Security.Claims;
using System.Threading.RateLimiting;
using Common.Authentication;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Nombre de la política CORS que aplica el pipeline más abajo.
const string FrontendCorsPolicy = "FrontendCors";

// (0) Logging estructurado con Serilog. Reemplaza el logger por defecto.
//     - ReadFrom.Configuration: lee niveles mínimos de la sección "Serilog" de appsettings.
//     - Enrich.FromLogContext: deja que el código adjunte propiedades por request.
//     - WriteTo.Console(CompactJsonFormatter): cada evento sale como UNA línea JSON
//       (formato CLEF) a stdout, que Docker captura y un agregador puede consultar.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

// (1) Registra el motor de YARP y le dice que lea sus rutas/clusters
//     desde la sección "ReverseProxy" de appsettings.json.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// (1b) CORS para frontends web (browser). El cliente WinForms de escritorio
//      ignora CORS, pero un futuro frontend web sí lo necesita. Los orígenes
//      permitidos se leen de "Cors:AllowedOrigins" (lista blanca por entorno).
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // permite enviar el Authorization/cookies desde el browser
});

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

// (logging) Una línea-resumen por request (método, ruta, status, duración) en vez
// del ruido multi-línea por defecto. Va PRIMERO para envolver todo el pipeline:
// la línea se emite al completar la request, cuando User/Connection ya están listos,
// así que el callback enriquece con UserId e IP de quien llamó.
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
            diagnosticContext.Set("UserId", userId);
        diagnosticContext.Set("ClientIp",
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    };
});

// Endpoint propio del gateway (NO se reenvía a nadie): chequeo rápido de vida.
app.MapGet("/", () => "API Gateway up");

// Orden del pipeline (importa): CORS -> autenticar -> autorizar -> rate limit -> proxy.
// CORS va primero para que responda los preflight (OPTIONS) antes de auth/rate-limit.
// Auth va ANTES del rate limiter para que la clave "per-user" ya tenga el userId.
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Enchufa YARP. Cada Route aplica su AuthorizationPolicy/RateLimiterPolicy
// (si las declara) antes de reenviar al Cluster.
app.MapReverseProxy();

app.Run();
