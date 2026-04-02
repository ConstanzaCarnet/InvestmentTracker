using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Holdings.API.EventBusConsumer;
using EventBus.Messages.Events;
using Holdings.Infrastructure.Repositories;
using Holdings.Infrastructure.ExternalServices;
using Holdings.Infrastructure.Data;
using Holdings.Infrastructure.Services;
using Holdings.Application.Interfaces;
using Holdings.API.Services;
using Polly;
using Polly.Extensions.Http;
using Holdings.Infrastructure.Http;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE SERVICIOS CORE ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Solo una vez

// --- 2. BASE DE DATOS Y REPOS ---
builder.Services.AddDbContext<HoldingsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IHoldingRepository, HoldingRepository>();
builder.Services.AddScoped<IHoldingsService, HoldingsService>();


// --- 3. CLIENTES HTTP ---
builder.Services.AddHttpClient<IMarketDataClient, MarketDataClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MarketData"]!);
})
.AddPolicyHandler(HttpPolicies.GetRetryPolicy())
.AddPolicyHandler(HttpPolicies.GetTimeoutPolicy());
/*builder.Services.AddHttpClient<IMarketDataClient, MarketDataClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5003/");
});
*/

// --- 4. MASSTRANSIT / RABBITMQ ---
var eventBusSettings = builder.Configuration.GetSection("EventBusSettings");
var hostAddress = eventBusSettings["HostAddress"];

builder.Services.AddMassTransit(config =>
{
    // Registramos los consumidores para cada evento relevante
    config.AddConsumer<TransactionCreatedConsumer>();
    config.AddConsumer<TransactionUpdatedConsumer>();
    config.AddConsumer<TransactionDeletedConsumer>();
    config.AddConsumer<UserCreatedConsumer>();

    config.UsingRabbitMq((ctx, cfg) =>
    {
        if (string.IsNullOrEmpty(hostAddress))
            throw new InvalidOperationException("RabbitMQ HostAddress no está configurado.");

        cfg.Host(hostAddress);
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

//----- 5. CREO MIGRATIONS AL LEVANTAR EL SERVICIO(opcional pero recomendado) -----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HoldingsDbContext>();
    db.Database.Migrate();
}

// --- 5. PIPELINE DE MIDDLEWARE ---
// IMPORTANTE: En Docker, a veces el entorno no es "Development". 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
// app.UseHttpsRedirection(); // Sugerencia: Coméntalo si estás probando local en Docker sin certificados SSL configurados

app.Run();