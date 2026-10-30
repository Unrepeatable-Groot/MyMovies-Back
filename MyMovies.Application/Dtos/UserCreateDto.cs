namespace MyMovies.Application.Dtos;

public class UserCreateDto
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Code { get; set; } = null!;
}
