using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMovies.Application.Dtos;
using MyMovies.Application.Email;
using MyMovies.Application.Interfaces;

namespace MyMovies.Controllers;


[ApiController]
[Route("api/[controller]")]

public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IEmailVerificationService _verificationService;
    private readonly IValidator<UserCreateDto> _validator;

    public UserController(IUserService userService,
        IEmailVerificationService verificationService,
        IValidator<UserCreateDto> validator)
    {
        _userService = userService;
        _verificationService = verificationService;
        _validator = validator;
    }


    [HttpPost("send-code")]
    public async Task<IActionResult> SendCodeAsync([FromBody] SendCodeDto dto)
    {
        var validation = await _validator.ValidateAsync(new UserCreateDto
        {
            Username = dto.Username,
            Email = dto.Email,
            Password = dto.Password,
            Code = "00000"
        });
        if (!validation.IsValid)
            return BadRequest(validation.Errors[0].ErrorMessage);

        var availabilityError = await _userService
            .CheckRegistrationAvailabilityAsync(dto.Username, dto.Email);
        if (availabilityError != null)
            return BadRequest(availabilityError);

        var sendError = await _verificationService.SendCodeAsync(dto.Email);
        if (sendError != null)
            return BadRequest(sendError);

        return Ok();
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] UserCreateDto dto)
    {
        var codeError = _verificationService.Verify(dto.Email, dto.Code);
        if (codeError != null)
            return BadRequest(codeError);

        var token = await _userService.RegisterAsync(dto);
        _verificationService.Remove(dto.Email);
        return Ok(token);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto dto)
    {
        var token = await _userService.LoginAsync(dto);
        return Ok(token);
    }


    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordDto dto)
    {
        if (!await _userService.EmailExistsAsync(dto.Email))
            return BadRequest("No account found with this email.");

        var sendError = await _verificationService.SendCodeAsync(dto.Email);
        if (sendError != null)
            return BadRequest(sendError);

        return Ok();
    }

    [HttpPost("verify-reset-code")]
    public IActionResult VerifyResetCode([FromBody] VerifyResetCodeDto dto)
    {
        var error = _verificationService.Verify(dto.Email, dto.Code);
        if (error != null)
            return BadRequest(error);

        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordDto dto)
    {
        var codeError = _verificationService.Verify(dto.Email, dto.Code);
        if (codeError != null)
            return BadRequest(codeError);

        var validation = await _validator.ValidateAsync(
            new UserCreateDto { Password = dto.NewPassword },
            options => options.IncludeProperties(x => x.Password));
        if (!validation.IsValid)
            return BadRequest(validation.Errors[0].ErrorMessage);

        var token = await _userService.ResetPasswordAsync(dto.Email, dto.NewPassword);
        _verificationService.Remove(dto.Email);
        return Ok(token);
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAsync()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound($"User with ID {id} not found!");
        return Ok(user);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UserUpdateDto dto)
    {
        var user = await _userService.UpdateAsync(id, dto);
        return Ok(user);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        await _userService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("{id}/change-email")]
    [Authorize]
    public async Task<IActionResult> ChangeEmailAsync(int id, [FromBody] ChangeEmailDto dto)
    {
        await _userService.ChangeEmailAsync(id, dto);
        return Ok();
    }

    [HttpPut("{id}/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePasswordAsync(int id, [FromBody] ChangePasswordDto dto)
    {
        await _userService.ChangePasswordAsync(id, dto);
        return Ok();
    }

    [HttpPut("{id}/change-role")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> ChangeRoleAsync(int id, [FromBody] UserRoleUpdateDto dto)
    {
        await _userService.ChangeRoleAsync(id, dto);
        return Ok();
    }
}

