using DMS.Application.Events;
using DMS.Domain.Entities;
using DMS.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMS.Infrastructure.Messaging;

public sealed class SalesOrderApprovedConsumer(
    ApplicationDbContext dbContext,
    ILogger<SalesOrderApprovedConsumer> logger) : IConsumer<SalesOrderApprovedEvent>
{
    public async Task Consume(ConsumeContext<SalesOrderApprovedEvent> context)
    {
        const string handler = nameof(SalesOrderApprovedConsumer);
        var messageId = context.Message.MessageId;

        if (await dbContext.ProcessedMessages.AnyAsync(x => x.Id == messageId && x.Handler == handler, context.CancellationToken))
        {
            logger.LogInformation("Skipping duplicate SalesOrderApprovedEvent {MessageId}", messageId);
            return;
        }

        dbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            Id = messageId,
            Handler = handler,
            ProcessedOn = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Handled SalesOrderApprovedEvent for sales order {SalesOrderId}", context.Message.SalesOrderId);
    }
}
