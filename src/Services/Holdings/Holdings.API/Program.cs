using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Holdings.API.EventBusConsumer;
using Holdings.Infrastructure.Repositories;
using Holdings.Infrastructure.ExternalServices;
using Holdings.Infrastructure.Data;
using Holdings.Application.Interfaces;
using Holdings.Application.Services;
using Holdings.Infrastructure.Http;
using Common.Authentication;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CORE ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Valida los JWT que emite Users (mismo Key/Issuer/Audience vía sección "Jwt").
builder.Services.AddJwtAuthentication(builder.Configuration);

// --- 2. BASE DE DATOS Y REPOS ---
builder.Services.AddDbContext<HoldingsDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    // EF Core 9 eleva PendingModelChangesWarning a error; lo ignoramos porque
    // la migration EFCore9SnapshotFix ya sincroniza el snapshot.
    options.ConfigureWarnings(w =>
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddScoped<IHoldingRepository, HoldingRepository>();
builder.Services.AddScoped<IHoldingsService, PortfolioService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();

// --- 3. CLIENTES HTTP ---
builder.Services.AddHttpClient<IMarketDataClient, MarketDataClient>(client =>
{
    var baseUrl = builder.Configuration["Services:MarketData"]
        ?? throw new InvalidOperationException("Services:MarketData is not configured.");
    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(HttpPolicies.GetRetryPolicy())
.AddPolicyHandler(HttpPolicies.GetTimeoutPolicy());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// --- 4. MASSTRANSIT / RABBITMQ ---
var hostAddress = builder.Configuration["EventBusSettings:HostAddress"]
    ?? throw new InvalidOperationException("RabbitMQ HostAddress no está configurado.");

builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<TransactionCreatedConsumer>();
    config.AddConsumer<TransactionUpdatedConsumer>();
    config.AddConsumer<TransactionDeletedConsumer>();
    config.AddConsumer<UserCreatedConsumer>();

    config.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(hostAddress);
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

// --- 5. MIGRATIONS CON RETRY ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HoldingsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (var attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Database migrated successfully.");
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning("Migration attempt {Attempt}/10 failed: {Message}. Waiting 5s...", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

// --- 6. PIPELINE ---
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();   // lee y valida el token → rellena User.Claims
app.UseAuthorization();    // evalúa [Authorize]
app.MapControllers();

app.Run();
