using MassTransit;
using MarketData.Application.Interfaces;
using MarketData.Application.Services;
using MarketData.Infrastructure.Providers;
using MarketData.Infrastructure.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<YahooFinanceClient>();
builder.Services.AddScoped<IPriceProvider, YahooFinancePriceProvider>();
builder.Services.AddScoped<ICachedPriceService, CachedPriceService>();
builder.Services.AddScoped<IPriceService, PriceService>();

builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["EventBusSettings:HostAddress"]);
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();
