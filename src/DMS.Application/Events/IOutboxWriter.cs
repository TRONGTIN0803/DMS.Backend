namespace DMS.Application.Events;

public interface IOutboxWriter
{
    void Add<TEvent>(TEvent integrationEvent, Guid messageId)
        where TEvent : notnull;
}
