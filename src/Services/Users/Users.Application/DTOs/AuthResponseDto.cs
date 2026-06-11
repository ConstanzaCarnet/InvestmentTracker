namespace Users.Application.DTOs;

// Lo que devolvemos tras un login exitoso. El cliente guarda AccessToken
// y lo manda como "Authorization: Bearer {AccessToken}" en cada request.
public record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email
);
