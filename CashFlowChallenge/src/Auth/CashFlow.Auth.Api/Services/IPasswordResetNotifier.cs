namespace CashFlow.Auth.Api.Services;

public interface IPasswordResetNotifier
{
    Task SendAsync(string email, string token, CancellationToken cancellationToken = default);
}
