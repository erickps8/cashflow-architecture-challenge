using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Worker.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace CashFlow.Tests;

public class WorkerTests
{
    [Fact]
    public async Task Worker_Should_Publish_Pending_Message()
    {
        var message = new OutboxMessage { Payload = "{ \"test\": true }" };

        var repository = new Mock<IOutboxMessageRepository>();
        var publisher = new Mock<IRabbitMqPublisher>();

        repository.Setup(x => x.GetPendingAsync())
            .ReturnsAsync([message]);

        var services = new ServiceCollection();
        services.AddScoped(_ => repository.Object);
        services.AddScoped(_ => publisher.Object);

        var worker = new CashFlow.Worker.Worker(
            services.BuildServiceProvider(),
            Mock.Of<ILogger<CashFlow.Worker.Worker>>());

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        await worker.StartAsync(cts.Token);

        await Task.Delay(200);

        message.ProcessedAt.Should().NotBeNull();
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
        publisher.Verify(x => x.PublishAsync(message.Payload), Times.Once);
    }

    [Fact]
    public async Task Worker_Should_Increment_Retry_When_Publish_Fails()
    {
        var message = new OutboxMessage { Payload = "{ \"test\": true }" };

        var repository = new Mock<IOutboxMessageRepository>();
        var publisher = new Mock<IRabbitMqPublisher>();

        repository.Setup(x => x.GetPendingAsync())
            .ReturnsAsync([message]);

        publisher.Setup(x => x.PublishAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("rabbit unavailable"));

        var services = new ServiceCollection();
        services.AddScoped(_ => repository.Object);
        services.AddScoped(_ => publisher.Object);

        var worker = new CashFlow.Worker.Worker(
            services.BuildServiceProvider(),
            Mock.Of<ILogger<CashFlow.Worker.Worker>>());

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        await worker.StartAsync(cts.Token);

        await Task.Delay(200);

        message.RetryCount.Should().Be(1);
        message.Error.Should().Be("rabbit unavailable");
        repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}