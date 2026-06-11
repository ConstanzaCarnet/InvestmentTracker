using Microsoft.EntityFrameworkCore;
using Users.Infrastructure;
using Users.Application.Interfaces;
using Users.Infrastructure.Data;
using Users.Infrastructure.Repositories;
using Users.Application.Services;
using Users.API.Middleware;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
// Obtenemos la Connection String del archivo de configuraci�n o variables de entorno (Docker)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var eventBusSettings = builder.Configuration.GetSection("EventBusSettings");
var hostAddress = eventBusSettings["HostAddress"];

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Registro de Repositorios
//con AddScoped se crea una instancia por cada petici�n HTTP
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddControllers();
// Configuraci�n de MassTransit con RabbitMQ
builder.Services.AddMassTransit(x =>
{
    //x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Si hostAddress es nulo o vac�o, fallar� r�pido avis�ndote
        if (string.IsNullOrEmpty(hostAddress))
        {
            throw new InvalidOperationException("RabbitMQ HostAddress no est� configurado en appsettings o variables de entorno.");
        }
        cfg.Host(hostAddress);
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddInfrastructureServices(builder.Configuration);


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    //con esto aseguro que la base de datos est� creada y migrada
    //y que no falle si el contenedor de la base de datos a�n no est� listo
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<UsersDbContext>();

    var retries = 10;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Intentando aplicar migraciones... (Intentos restantes: {Retries})", retries);
            dbContext.Database.Migrate();
            logger.LogInformation("Base de datos migrada con �xito.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning("La base de datos no est� lista todav�a, reintentando en 5 segundos...");
            Thread.Sleep(5000);

            if (retries == 0)
            {
                logger.LogCritical("No se pudo conectar a la base de datos tras varios intentos: {Message}", ex.Message);
                throw; 
            }
        }
    }
}
// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
//app.UseHttpsRedirection();
app.Run();

