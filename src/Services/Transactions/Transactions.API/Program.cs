using Microsoft.EntityFrameworkCore;
using MassTransit;
using Transactions.Infrastructure.Data;
using Transactions.Application.Interfaces;
using Transactions.Infrastructure.Repositories;
using Transactions.API.Extensions;
using Transactions.API.Services;
using Transactions.API.Middleware;
using Transactions.API.BackgroundServices;
using Common.Authentication;

var builder = WebApplication.CreateBuilder(args);
var eventBusSettings = builder.Configuration.GetSection("EventBusSettings");
var hostAddress = eventBusSettings["HostAddress"];
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Valida los JWT que emite Users (mismo Key/Issuer/Audience vía sección "Jwt").
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TransactionConnectionString")));

builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
{
    // Esto permite que el JSON acepte "BUY" o "SELL" como strings y los convierta al Enum de C#
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

//configurando MassTransit con RabbitMQ
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((ctx, cfg) =>
    {
        if (string.IsNullOrEmpty(hostAddress))
        {
            throw new InvalidOperationException("RabbitMQ HostAddress no est� configurado en appsettings o variables de entorno.");
        }
        cfg.Host(hostAddress);
        cfg.ConfigureEndpoints(ctx);
    });
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Retry de migración: en un `docker compose up` en frío SQL Server puede no estar
    // listo todavía. Sin esto el servicio crashea al arrancar (race con la BD).
    for (var attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await InstrumentsSeeder.SeedAsync(db);
            logger.LogInformation("Database migrated and seeded successfully.");
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning("Migration attempt {Attempt}/10 failed: {Message}. Waiting 5s...", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();   // lee y valida el token → rellena User.Claims
app.UseAuthorization();    // evalúa [Authorize]
app.MapControllers();
//app.UseHttpsRedirection();
app.Run();
