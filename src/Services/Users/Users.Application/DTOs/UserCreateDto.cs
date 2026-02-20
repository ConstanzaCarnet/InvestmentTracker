namespace Users.Application.DTOs;
//usamos record porque es inmutable y solo tiene datos
public record UserCreateDto(
    string Email,
    string FirstName,
    string LastName,
    string Password
);