using System.Text.Json;
using DMS.Application.Events;
using DMS.Domain.Entities;
using DMS.Infrastructure.Persistence;

namespace DMS.Infrastructure.Messaging;

public sealed class OutboxWriter(ApplicationDbContext dbContext) : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public void Add<TEvent>(TEvent integrationEvent, Guid messageId)
        where TEvent : notnull
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = messageId,
            Type = typeof(TEvent).FullName ?? typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            OccurredOn = DateTimeOffset.UtcNow
        });
    }
}
