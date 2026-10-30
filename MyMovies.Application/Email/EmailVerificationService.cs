using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyMovies.Application.Interfaces;

namespace MyMovies.Application.Email;

public class VerificationEntry
{
    public string Code { get; set; } = "";
    public int Attempts { get; set; }
    public DateTime SentAt { get; set; }
}

public interface IEmailVerificationService
{
    Task<string?> SendCodeAsync(string email);
    string? Verify(string email, string code);
    void Remove(string email);
}

public class EmailVerificationService : IEmailVerificationService
{
    private const int CodeLength = 5;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private const int ResendCooldownSeconds = 30;
    private const int MaxAttempts = 5;

    private readonly IMemoryCache _cache;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(IMemoryCache cache, IEmailSender emailSender,
        ILogger<EmailVerificationService> logger)
    {
        _cache = cache;
        _emailSender = emailSender;
        _logger = logger;
    }

    private static string Key(string email) => $"verify:{email.Trim().ToLowerInvariant()}";

    public async Task<string?> SendCodeAsync(string email)
    {
        var key = Key(email);

        if (_cache.TryGetValue(key, out VerificationEntry? old) &&
            old!.SentAt > DateTime.UtcNow.AddSeconds(-ResendCooldownSeconds))
            return "Please wait a few seconds before requesting a new code.";

        var max = (int)Math.Pow(10, CodeLength);
        var code = RandomNumberGenerator.GetInt32(0, max).ToString("D" + CodeLength);

        _cache.Set(key,
            new VerificationEntry { Code = code, SentAt = DateTime.UtcNow },
            CodeLifetime);

        try
        {
            await _emailSender.SendAsync(email.Trim(), "Your verification code",
                $"<p>Your verification code is:</p>" +
                $"<p style='font-size:28px;font-weight:bold;letter-spacing:6px'>{code}</p>" +
                $"<p>It is valid for {CodeLifetime.TotalMinutes} minutes.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email");
            _cache.Remove(key);
            return "Could not send the email. Please try again later.";
        }

        return null;
    }

    public string? Verify(string email, string code)
    {
        var key = Key(email);

        if (!_cache.TryGetValue(key, out VerificationEntry? entry) || entry == null)
            return "The code has expired. Please request a new one.";

        if (entry.Attempts >= MaxAttempts)
        {
            _cache.Remove(key);
            return "Too many attempts. Please request a new code.";
        }

        if (entry.Code != (code ?? "").Trim())
        {
            entry.Attempts++;
            return "Incorrect code.";
        }

        return null;
    }

    public void Remove(string email) => _cache.Remove(Key(email));
}