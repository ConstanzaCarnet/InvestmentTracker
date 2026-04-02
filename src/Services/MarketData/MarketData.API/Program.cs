using MassTransit;
using MarketData.Application.Interfaces;
using MarketData.Application.Services;
using MarketData.Infrastructure.Providers;
using MarketData.Infrastructure.Http;
using MarketData.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar HttpClient para el proveedor de precios
// 1.1 Registrar servicios de aplicación e infraestructura
builder.Services.AddScoped<IPriceProvider, FinnhubPriceProvider>();

builder.Services.AddDbContext<MarketDataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
//agregamos memoria caché para almacenar los datos de mercado temporalmente
builder.Services.AddMemoryCache();

builder.Services.AddScoped<CachedPriceService>();
builder.Services.AddHttpClient<FinnhubClient>();
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
builder.Services.AddSwaggerGen(); 

var app = builder.Build();

// Configurar Swagger para ver la API en el navegador
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();