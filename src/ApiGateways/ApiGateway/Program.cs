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

var app = builder.Build();

// Endpoint propio del gateway (NO se reenvía a nadie): sirve para chequear
// de un vistazo que el gateway está vivo.
app.MapGet("/", () => "API Gateway up");

// (3) Orden del pipeline (importa): autenticar -> autorizar -> proxy.
//     Las rutas con "AuthorizationPolicy": "default" en appsettings rechazan
//     aquí (401) los tokens ausentes/inválidos, antes de tocar el backend.
app.UseAuthentication();
app.UseAuthorization();

// (4) Enchufa YARP. Toda request que matchee una Route se reenvía a su Cluster
//     (las protegidas, solo si ya pasaron la autorización de arriba).
app.MapReverseProxy();

app.Run();
