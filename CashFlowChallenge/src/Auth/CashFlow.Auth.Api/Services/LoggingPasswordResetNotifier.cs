namespace CashFlow.Auth.Api.Services;

public sealed class LoggingPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly ILogger<LoggingPasswordResetNotifier> _logger;

    public LoggingPasswordResetNotifier(ILogger<LoggingPasswordResetNotifier> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Password reset requested for {Email}. Configure a production email provider before release. Development token: {Token}",
            email,
            token);

        return Task.CompletedTask;
    }
}
