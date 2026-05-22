using System.Text;
using System.Text.Json;
using CashFlow.Consolidation.Domain.Interfaces;
using CashFlow.Launch.Domain.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CashFlow.Worker.Services;

public class EntryCreatedConsumer : BackgroundService
{
    private const string ExchangeName = "cashflow.exchange";
    private const string QueueName = "cashflow.entry-created.queue";
    private const string RoutingKey = "EntryCreatedEvent";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EntryCreatedConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public EntryCreatedConsumer(
        IServiceProvider serviceProvider,
        ILogger<EntryCreatedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "EntryCreatedConsumer iniciou.");

        var factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        _connection =
            await factory.CreateConnectionAsync(stoppingToken);

        _channel =
            await _connection.CreateChannelAsync(
                cancellationToken: stoppingToken);

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
            try
            {
                _logger.LogInformation(
                    "Mensagem recebida do RabbitMQ.");

                var body =
                    Encoding.UTF8.GetString(ea.Body.ToArray());

                var entryCreatedEvent =
                    JsonSerializer.Deserialize<EntryCreatedEvent>(body);

                if (entryCreatedEvent is null)
                {
                    throw new InvalidOperationException(
                        "Não foi possível desserializar EntryCreatedEvent.");
                }

                using var scope =
                    _serviceProvider.CreateScope();

                var service =
                    scope.ServiceProvider
                        .GetRequiredService<IDailyConsolidationService>();

                await service.ProcessEntryAsync(
                    entryCreatedEvent.Amount,
                    entryCreatedEvent.Type,
                    entryCreatedEvent.OccurredAt);

                _logger.LogInformation(
                    "Consolidação atualizada.");

                await _channel.BasicAckAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);

                _logger.LogInformation(
                    "EntryCreatedEvent processado com sucesso. EntryId: {EntryId}",
                    entryCreatedEvent.EntryId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                _logger.LogError(
                    ex,
                    "Erro ao processar EntryCreatedEvent.");

                await _channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
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