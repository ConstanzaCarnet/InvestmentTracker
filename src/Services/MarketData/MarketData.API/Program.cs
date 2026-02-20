using MarketData.API.Interfaces;
using MarketData.API.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar HttpClient para el proveedor de precios
builder.Services.AddHttpClient<IMarketProvider, DolarProvider>();

// 2. Configurar MassTransit (Solo si este servicio va a publicar eventos en el futuro)
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["EventBusSettings:HostAddress"]);
    });
});

// 3. Agregar servicios básicos de API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Cambié OpenApi por Swagger por compatibilidad estándar

var app = builder.Build();

// Configurar Swagger para ver la API en el navegador
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint para consultar precios
app.MapGet("/api/market/{ticker}", async (string ticker, IMarketProvider provider) =>
{
    try
    {
        var price = await provider.GetPriceAsync(ticker.ToUpper());
        return price is not null ? Results.Ok(price) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error obteniendo precio: {ex.Message}");
    }
});

app.Run();