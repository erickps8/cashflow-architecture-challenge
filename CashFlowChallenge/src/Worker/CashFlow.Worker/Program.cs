using CashFlow.Consolidation.Infra.Extensions;
using CashFlow.Launch.Infrastructure.Extensions;
using CashFlow.Worker;
using CashFlow.Worker.Configurations;
using CashFlow.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddLaunchInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddConsolidationInfrastructure(
    builder.Configuration.GetConnectionString("ConsolidationConnection")!);

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddHostedService<EntryCreatedConsumer>();

builder.Services.AddScoped<RabbitMqPublisher>();

var host = builder.Build();
host.Run();