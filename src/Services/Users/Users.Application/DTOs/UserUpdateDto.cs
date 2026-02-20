namespace Users.Application.DTOs;
//en este caso los atributos son opcionales porque puede que solo queramos actualizar uno de ellos
public record UserUpdateDto(
    string? FirstName,
    string? LastName,
    string? Password
);