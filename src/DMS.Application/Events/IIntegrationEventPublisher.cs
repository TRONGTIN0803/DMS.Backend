namespace DMS.Application.Events;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(string messageType, string payload, Guid messageId, CancellationToken cancellationToken = default);
}
