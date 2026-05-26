using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Worker.Services;

namespace CashFlow.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceProvider serviceProvider,
        ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();

            var repository =
                scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();

            var publisher =
                scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

            var pendingMessages = await repository.GetPendingAsync();

            foreach (var message in pendingMessages)
            {
                try
                {
                    await publisher.PublishAsync(message.Payload);

                    message.ProcessedAt = DateTime.UtcNow;

                    await repository.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    message.RetryCount++;

                    message.Error = ex.Message;

                    await repository.SaveChangesAsync();

                    _logger.LogError(ex,
                        "Erro ao publicar mensagem da Outbox");
                }
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}