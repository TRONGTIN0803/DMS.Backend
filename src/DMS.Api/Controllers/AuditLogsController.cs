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
[Route("api/v{version:apiVersion}/audit-logs")]
public sealed class AuditLogsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogResponse>>> GetAuditLogs([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        var auditLogsQuery = dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            auditLogsQuery = auditLogsQuery.Where(x =>
                x.EntityName.ToLower().Contains(keyword) ||
                x.EntityId.ToLower().Contains(keyword) ||
                x.Action.ToLower().Contains(keyword));
        }

        var totalCount = await auditLogsQuery.CountAsync(cancellationToken);
        var auditLogs = await auditLogsQuery
            .OrderByDescending(x => x.OccurredOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogResponse(
                x.Id,
                x.UserId,
                x.OccurredOn,
                x.EntityName,
                x.EntityId,
                x.Action,
                x.OldValue,
                x.NewValue))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<AuditLogResponse>(auditLogs, page, pageSize, totalCount));
    }
}

public sealed record AuditLogResponse(
    long Id,
    string? UserId,
    DateTimeOffset OccurredOn,
    string EntityName,
    string EntityId,
    string Action,
    string? OldValue,
    string? NewValue);
