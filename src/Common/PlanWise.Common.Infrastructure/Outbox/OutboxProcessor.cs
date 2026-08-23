using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Common.Infrastructure.Serialization;

namespace PlanWise.Common.Infrastructure.Outbox;

// Trimmed outbox dispatcher: no message bus, no cross-process delivery. Domain events raised via
// Entity.Raise() are persisted by InsertOutboxMessagesInterceptor in the same transaction as the
// business write, then this poller drains them in-process to whichever IDomainEventHandler<T>
// implementations are registered in the given assembly. Sufficient for in-module reactions (e.g.
// building an activity feed); revisit toward a real bus only if genuine cross-service delivery
// is ever needed.
internal sealed class OutboxProcessor<TDbContext>(
    IServiceScopeFactory scopeFactory,
    Assembly domainEventHandlersAssembly,
    OutboxProcessorOptions options,
    ILogger<OutboxProcessor<TDbContext>> logger)
    : BackgroundService
    where TDbContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.PollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Unhandled error while processing outbox messages for {DbContext}", typeof(TDbContext).Name);
            }
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        IDateTimeProvider dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        List<OutboxMessage> messages = await dbContext.Set<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (OutboxMessage message in messages)
        {
            await ProcessMessageAsync(message, scope.ServiceProvider, dateTimeProvider, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IServiceProvider serviceProvider,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            IDomainEvent? domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(message.Content, SerializerSettings.Instance);
            if (domainEvent is null)
            {
                message.MarkFailed(dateTimeProvider.UtcNow, $"Could not deserialize outbox message of type {message.Type}");
                return;
            }

            IEnumerable<IDomainEventHandler> handlers = DomainEventHandlersFactory.GetHandlers(
                domainEvent.GetType(), serviceProvider, domainEventHandlersAssembly);

            foreach (IDomainEventHandler handler in handlers)
            {
                await handler.Handle(domainEvent, cancellationToken);
            }

            message.MarkProcessed(dateTimeProvider.UtcNow);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error processing outbox message {MessageId} of type {MessageType}", message.Id, message.Type);
            message.MarkFailed(dateTimeProvider.UtcNow, exception.Message);
        }
    }
}
