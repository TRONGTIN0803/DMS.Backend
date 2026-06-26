using System.Text.Json;
using DMS.Application.Events;
using MassTransit;

namespace DMS.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(string messageType, string payload, Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageType.EndsWith(nameof(SalesOrderApprovedEvent), StringComparison.Ordinal))
        {
            var integrationEvent = JsonSerializer.Deserialize<SalesOrderApprovedEvent>(payload, SerializerOptions)
                ?? throw new InvalidOperationException("Invalid SalesOrderApprovedEvent payload.");
            await publishEndpoint.Publish(integrationEvent, cancellationToken);
            return;
        }

        if (messageType.EndsWith(nameof(InventoryUpdatedEvent), StringComparison.Ordinal))
        {
            var integrationEvent = JsonSerializer.Deserialize<InventoryUpdatedEvent>(payload, SerializerOptions)
                ?? throw new InvalidOperationException("Invalid InventoryUpdatedEvent payload.");
            await publishEndpoint.Publish(integrationEvent, cancellationToken);
            return;
        }

        if (messageType.EndsWith(nameof(InvoiceCreatedEvent), StringComparison.Ordinal))
        {
            var integrationEvent = JsonSerializer.Deserialize<InvoiceCreatedEvent>(payload, SerializerOptions)
                ?? throw new InvalidOperationException("Invalid InvoiceCreatedEvent payload.");
            await publishEndpoint.Publish(integrationEvent, cancellationToken);
            return;
        }

        throw new InvalidOperationException($"Unsupported integration event type '{messageType}'.");
    }
}
