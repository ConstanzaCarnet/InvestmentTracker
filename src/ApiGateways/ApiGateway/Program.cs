var builder = WebApplication.CreateBuilder(args);

// (1) Registra el motor de YARP y le dice que lea sus rutas/clusters
//     desde la sección "ReverseProxy" de appsettings.json.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Endpoint propio del gateway (NO se reenvía a nadie): sirve para chequear
// de un vistazo que el gateway está vivo.
app.MapGet("/", () => "API Gateway up");

// (2) Enchufa YARP al pipeline. A partir de acá, toda request que matchee
//     una Route se reenvía (reverse proxy) a su Cluster.
app.MapReverseProxy();

app.Run();
