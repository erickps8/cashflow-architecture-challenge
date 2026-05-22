using System.Text;
using CashFlow.Worker.Configurations;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CashFlow.Worker.Services;

public class RabbitMqPublisher
{
    private const string QueueName = "cashflow.entry-created.queue";

    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        IOptions<RabbitMqSettings> options,
        ILogger<RabbitMqPublisher> logger)
    {
        _settings = options.Value;
        _logger = logger;
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

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: _settings.Exchange,
            routingKey: _settings.RoutingKey);

        var body = Encoding.UTF8.GetBytes(message);

        _logger.LogInformation(
            "Publicando RabbitMQ. Exchange: {Exchange} | RoutingKey: {RoutingKey}",
            _settings.Exchange,
            _settings.RoutingKey);

        await channel.BasicPublishAsync(
            exchange: _settings.Exchange,
            routingKey: _settings.RoutingKey,
            mandatory: true,
            body: body);
    }
}