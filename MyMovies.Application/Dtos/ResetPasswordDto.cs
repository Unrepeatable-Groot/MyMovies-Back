namespace MyMovies.Application.Dtos;

public class ForgotPasswordDto
{
    public string Email { get; set; } = null!;
}

public class VerifyResetCodeDto
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
}

public class ResetPasswordDto
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}