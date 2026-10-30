using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using MyMovies.Application.Interfaces;

namespace MyMovies.Application.Email;

public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public BrevoEmailSender(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string html)
    {
        var apiKey = _config["Brevo:ApiKey"]
            ?? throw new InvalidOperationException("Brevo:ApiKey is missing.");
        var senderEmail = _config["Brevo:SenderEmail"]
            ?? throw new InvalidOperationException("Brevo:SenderEmail is missing.");
        var senderName = _config["Brevo:SenderName"] ?? "MyMovies";

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        req.Headers.Add("api-key", apiKey);
        req.Content = JsonContent.Create(new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = to } },
            subject,
            htmlContent = html
        });

        var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            throw new Exception($"Brevo error {(int)res.StatusCode}: {body}");
        }
    }
}