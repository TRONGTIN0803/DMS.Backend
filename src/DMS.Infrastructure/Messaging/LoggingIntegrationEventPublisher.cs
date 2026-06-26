using DMS.Application.Events;
using Microsoft.Extensions.Logging;

namespace DMS.Infrastructure.Messaging;

public sealed class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    public Task PublishAsync(string messageType, string payload, Guid messageId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Published integration event {MessageId} of type {MessageType}", messageId, messageType);
        return Task.CompletedTask;
    }
}
