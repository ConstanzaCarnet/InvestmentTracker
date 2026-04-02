namespace Users.Application.DTOs;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }


    public UserDto() { }

    public UserDto(Guid id, string email, string firstName, string lastName)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}