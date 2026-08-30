using System.Net;
using System.Net.Mail;

namespace CashFlow.Auth.Api.Services;

public sealed class LoggingPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoggingPasswordResetNotifier> _logger;

    public LoggingPasswordResetNotifier(IConfiguration configuration, ILogger<LoggingPasswordResetNotifier> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        var publicUrl = _configuration["App:PublicUrl"] ?? "https://plania.cloud";
        var resetUrl = $"{publicUrl.TrimEnd('/')}?resetEmail={Uri.EscapeDataString(email)}&resetToken={Uri.EscapeDataString(token)}";
        var host = _configuration["Email:Smtp:Host"];
        var from = _configuration["Email:From"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogError("Password reset email is not configured. Configure Email:Smtp and Email:From before release.");
            return;
        }

        var port = int.TryParse(_configuration["Email:Smtp:Port"], out var configuredPort) ? configuredPort : 587;
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var ssl) || ssl
        };

        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        using var message = new MailMessage(from, email)
        {
            Subject = "Redefinição de senha do CashFlow",
            Body = $"Recebemos uma solicitação para redefinir sua senha.\n\nAbra o link abaixo para criar uma nova senha:\n{resetUrl}\n\nO link expira em 30 minutos. Se você não fez esta solicitação, ignore este e-mail."
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
