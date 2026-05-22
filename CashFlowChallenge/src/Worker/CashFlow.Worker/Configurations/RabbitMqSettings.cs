namespace CashFlow.Worker.Configurations;

public class RabbitMqSettings
{
    public string HostName { get; set; } = string.Empty;

    public int Port { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Exchange { get; set; } = string.Empty;

    public string RoutingKey { get; set; } = string.Empty;
}