using MyMovies.Application.Dtos;

namespace MyMovies.Application.Interfaces;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<string> RegisterAsync(UserCreateDto dto);
    Task<string> LoginAsync(UserLoginDto dto);
    Task<UserDto> UpdateAsync(int id, UserUpdateDto dto);
    Task DeleteAsync(int id);
    Task ChangeEmailAsync(int id, ChangeEmailDto dto);
    Task ChangePasswordAsync(int id, ChangePasswordDto dto);
    Task ChangeRoleAsync(int id, UserRoleUpdateDto dto);
    Task<string?> CheckRegistrationAvailabilityAsync(string username, string email);
    Task<bool> EmailExistsAsync(string email);
    Task<string> ResetPasswordAsync(string email, string newPassword);
}
