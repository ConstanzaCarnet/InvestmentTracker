using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Holdings.API.EventBusConsumer;
using EventBus.Messages.Events;
using Holdings.Infrastructure.Repositories;
using Holdings.Infrastructure.ExternalServices;
using Holdings.Infrastructure.Data;
using Holdings.Application.Interfaces;
using Holdings.API.Services;

var builder = WebApplication.CreateBuilder(args);
var eventBusSettings = builder.Configuration.GetSection("EventBusSettings");
var hostAddress = eventBusSettings["HostAddress"];

builder.Services.AddDbContext<HoldingsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IHoldingRepository, HoldingRepository>();
builder.Services.AddScoped<IHoldingsService, HoldingsService>();
// Configuramos el cliente HTTP indicando que busque al contenedor de MarketData
builder.Services.AddHttpClient<IMarketDataClient, MarketDataClient>(client =>
{
    // "marketdata-service" es el nombre que pusiste en tu docker-compose
    client.BaseAddress = new Uri("http://marketdata-service:8080/");
});
builder.Services.AddScoped<IHoldingsService, HoldingsService>();
builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<TransactionCreatedConsumer>();

    config.UsingRabbitMq((ctx, cfg) =>
    {
        if (string.IsNullOrEmpty(hostAddress))
        {
            throw new InvalidOperationException("RabbitMQ HostAddress no está configurado.");
        }
        cfg.Host(hostAddress);

        // Deja que MassTransit maneje las colas automáticamente por ahora para evitar errores
        cfg.ConfigureEndpoints(ctx);

        // Si quieres una cola con nombre específico, asegúrate de que sea así:
        cfg.ReceiveEndpoint("transaction-created-queue", e =>
        {
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            e.ConfigureConsumer<TransactionCreatedConsumer>(ctx);
        });
    });
});
/*builder.Services.Configure<MassTransitHostOptions>(options =>
{
    options.WaitUntilStarted = true;
    options.StartTimeout = TimeSpan.FromSeconds(60);
    options.StopTimeout = TimeSpan.FromSeconds(30);
});*/
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.UseHttpsRedirection();
app.Run();
