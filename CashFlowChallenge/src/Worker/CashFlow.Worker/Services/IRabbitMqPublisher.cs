namespace CashFlow.Worker.Services;

public interface IRabbitMqPublisher
{
    Task PublishAsync(string message);
}