using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyMovies.Application.Dtos;
using MyMovies.Application.Interfaces;
using MyMovies.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyMovies.Application.Service;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }


    public async Task<string?> CheckRegistrationAvailabilityAsync(string username, string email)
    {
        if (await _userRepository.GetByEmailAsync(email) != null)
            return $"Email '{email}' is already registered.";

        if (await _userRepository.GetByUsernameAsync(username) != null)
            return $"Username '{username}' is already taken.";

        return null;
    }

    public async Task<string> RegisterAsync(UserCreateDto dto)
    {
        var existingByEmail = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingByEmail != null)
            throw new Exception($"Email '{dto.Email}' is already registered.");

        var existingByUsername = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existingByUsername != null)
            throw new Exception($"Username '{dto.Username}' is already taken.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
        };

        user.Password = _passwordHasher.HashPassword(user, dto.Password);
        await _userRepository.AddAsync(user);

        var loginDto = new UserLoginDto
        {
            Email = dto.Email,
            Password = dto.Password
        };
        return await LoginAsync(loginDto);
    }

    public async Task<string> LoginAsync(UserLoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            throw new Exception("Invalid credentials.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new Exception("Invalid credentials.");

        return GenerateToken(user);
    }


    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email) != null;
    }

    public async Task<string> ResetPasswordAsync(string email, string newPassword)
    {
        var user = await _userRepository.GetByEmailAsync(email)
            ?? throw new Exception("User not found.");

        user.Password = _passwordHasher.HashPassword(user, newPassword);
        await _userRepository.UpdateAsync(user);

        return GenerateToken(user);
    }

    public async Task ChangeEmailAsync(int id, ChangeEmailDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id)
        ?? throw new Exception($"User with ID {id} does not exist.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new Exception("Current password is incorrect.");

        var existingByEmail = await _userRepository.GetByEmailAsync(dto.NewEmail);
        if (existingByEmail != null && existingByEmail.Id != id)
            throw new Exception($"Email '{dto.NewEmail}' is already in use.");

        user.Email = dto.NewEmail;
        await _userRepository.UpdateAsync(user);
    }

    public async Task ChangePasswordAsync(int id, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new Exception($"User with ID {id} does not exist.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new Exception("Current password is incorrect.");

        user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);
        await _userRepository.UpdateAsync(user);
    }

    public async Task ChangeRoleAsync(int id, UserRoleUpdateDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id)
        ?? throw new Exception($"User with ID {id} does not exist.");

        user.Role = dto.Role;
        await _userRepository.UpdateAsync(user);
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(u => new UserDto(u)).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : new UserDto(user);
    }

    private string GenerateToken(User user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new Exception("JWT Key is missing.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryMinutesString = _configuration["Jwt:ExpiryMinutes"];

        int expiryMinutes = 240;
        if (!string.IsNullOrEmpty(expiryMinutesString) &&
            int.TryParse(expiryMinutesString, out var tmp))
            expiryMinutes = tmp;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),   
            new Claim(ClaimTypes.Name, user.Username)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<UserDto> UpdateAsync(int id, UserUpdateDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new Exception($"User with ID {id} does not exist.");

        var existingByUsername = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existingByUsername != null && existingByUsername.Id != id)
            throw new Exception($"Username '{dto.Username}' is already taken.");

        user.Username = dto.Username;
        if (dto.ProfileImageUrl != null)
            user.ProfileImageUrl = dto.ProfileImageUrl;

        await _userRepository.UpdateAsync(user);
        return new UserDto(user);
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id)
        ?? throw new Exception($"User with ID {id} does not exist.");

        await _userRepository.DeleteAsync(user);
    }
}
