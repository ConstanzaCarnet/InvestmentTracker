using Microsoft.AspNetCore.Mvc;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Application.Services;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;

    public UsersController(IUserService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(UserCreateDto dto)
    {
        var id = await _service.CreateUserAsync(dto);

        return Ok(new
        {
            Id = id,
            Message = "Usuario creado con éxito"
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _service.GetAllAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var user = await _service.GetByIdAsync(id);

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UserUpdateDto dto)
    {
        await _service.UpdateAsync(id, dto);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        return NoContent();
    }
}