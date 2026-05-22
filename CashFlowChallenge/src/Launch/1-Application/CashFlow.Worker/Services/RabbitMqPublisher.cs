using System.Text;
using CashFlow.Worker.Configurations;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CashFlow.Worker.Services;

public class RabbitMqPublisher
{
    private readonly RabbitMqSettings _settings;

    public RabbitMqPublisher(
        IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;
    }

    public async Task PublishAsync(string message)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        await using var connection = await factory.CreateConnectionAsync();

        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: _settings.Exchange,
            type: ExchangeType.Direct,
            durable: true);

        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: _settings.Exchange,
            routingKey: _settings.RoutingKey,
            body: body);
    }
}