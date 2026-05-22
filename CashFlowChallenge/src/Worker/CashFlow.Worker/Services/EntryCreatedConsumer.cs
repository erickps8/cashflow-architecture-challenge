using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CashFlow.Worker.Services;

public class EntryCreatedConsumer : BackgroundService
{
    private const string ExchangeName = "cashflow.exchange";
    private const string QueueName = "cashflow.entry-created.queue";
    private const string RoutingKey = "EntryCreatedEvent";

    private readonly ILogger<EntryCreatedConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public EntryCreatedConsumer(ILogger<EntryCreatedConsumer> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = "host.docker.internal",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);

        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            _logger.LogInformation(
                "Mensagem recebida do RabbitMQ: {Message}",
                body);

            await _channel.BasicAckAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                cancellationToken: stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Consumer iniciado. Queue: {QueueName}",
            QueueName);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}