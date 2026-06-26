using DMS.Api.Auth;
using DMS.Domain.Entities;
using DMS.Infrastructure.Persistence;
using DMS.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/outbox-messages")]
public sealed class OutboxMessagesController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<OutboxMessageResponse>>> GetOutboxMessages([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        var messagesQuery = dbContext.OutboxMessages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            messagesQuery = messagesQuery.Where(x => x.Type.ToLower().Contains(keyword));
        }

        var totalCount = await messagesQuery.CountAsync(cancellationToken);
        var messages = await messagesQuery
            .OrderByDescending(x => x.OccurredOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OutboxMessageResponse(
                x.Id,
                x.Type,
                x.Payload,
                x.OccurredOn,
                x.ProcessedOn,
                x.Error))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<OutboxMessageResponse>(messages, page, pageSize, totalCount));
    }
}

public sealed record OutboxMessageResponse(
    Guid Id,
    string Type,
    string Payload,
    DateTimeOffset OccurredOn,
    DateTimeOffset? ProcessedOn,
    string? Error);
