using Microsoft.AspNetCore.Mvc;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Infrastructure.Data;
using Users.Domain.Entities;
using EventBus.Messages.Events;
using MassTransit;

namespace Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UsersController> _logger;
    //con esto inyectamos el repositorio
    public UsersController(IUserRepository repository, IPublishEndpoint publishEndpoint, ILogger<UsersController> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [HttpPost]
    //IActionResult es una interfaz que representa el resultado de una acción de un controlador en ASP.NET Core.
    //lo usamos para devolver diferentes tipos de respuestas HTTP, por eso va en la capa API
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
    {
        // 1. Validar si el usuario ya existe
        var existingUser = await _repository.GetByEmailAsync(dto.Email);
        if (existingUser != null) return BadRequest("El correo ya está registrado.");

        // 2. IMPORTANTE: Hashear el password (usaremos un placeholder por ahora)
        // En un caso real aquí usarías BCrypt.Net
        var passwordHash = $"hashed_{dto.Password}";

        // 3. Crear la entidad
        var user = new User(dto.Email, dto.FirstName, dto.LastName, passwordHash);

        // 4. Guardar
        await _repository.AddAsync(user);
        await _repository.SaveChangesAsync();
        // 1.1. Loguear la creación del usuario
        _logger.LogInformation("Usuario {Id} guardado en la base de datos.", user.Id);
        // 1.2. Publicar el evento en RabbitMQ
        await _publishEndpoint.Publish(new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
        // 1.3. Loguear la publicación del evento
        _logger.LogInformation("Evento UserCreatedEvent publicado para el usuario {Id}.", user.Id);
        // 5. Devolver resultado (sin el password)
        return Ok(new { user.Id, user.Email, Message = "Usuario creado con éxito" });
    }
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _repository.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.UpdateInfo(dto.FirstName, dto.LastName);
        user.UpdatePassword(dto.Password); // Recuerda hashear en un caso real
        await _repository.UpdateAsync(user);
        await _repository.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NotFound();
        await _repository.DeleteAsync(user);
        await _repository.SaveChangesAsync();
        //publico el evento de usuario eliminado
        await _publishEndpoint.Publish(new UserDeletedEvent
        {
            UserId = user.Id,
            Email = user.Email
        });
        return NoContent();
    }
}