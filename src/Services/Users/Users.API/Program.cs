using Microsoft.EntityFrameworkCore;
using Users.Infrastructure;
using Users.Application.Interfaces;
using Users.Infrastructure.Data;
using Users.Infrastructure.Repositories;
using Users.Application.Services;
using Users.API.Middleware;
using Users.Infrastructure.Authentication;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Obtenemos la Connection String del archivo de configuraci�n o variables de entorno (Docker)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var eventBusSettings = builder.Configuration.GetSection("EventBusSettings");
var hostAddress = eventBusSettings["HostAddress"];

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddEndpointsApiExplorer();

// Swagger con soporte para "Authorize": un botón donde pegás el token y queda en todas las requests.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegá solo el token JWT (sin el prefijo 'Bearer ')."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --- AUTENTICACIÓN JWT ---
// Leemos la sección "Jwt". Si falta la clave, fallamos al arrancar (mejor que un 500 difuso después).
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Falta la sección 'Jwt' en la configuración.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres (256 bits para HMAC-SHA256).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Estas reglas se aplican a CADA request con un token. Deben coincidir con
        // lo que pusimos al firmar en JwtTokenGenerator (mismo secreto, issuer y audience).
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,            // rechaza tokens expirados
            ClockSkew = TimeSpan.FromSeconds(30) // tolerancia de reloj (default son 5 min)
        };
    });
builder.Services.AddAuthorization();
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

// El orden importa: primero AUTENTICAR (¿quién sos? → lee el token, rellena User.Claims),
// luego AUTORIZAR (¿podés entrar? → evalúa [Authorize]).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
//app.UseHttpsRedirection();
app.Run();

