using DMS.Application.Orders;
using DMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DMS.Infrastructure.Orders;

public sealed class PostgresOrderNumberGenerator(ApplicationDbContext dbContext) : IOrderNumberGenerator
{
    public async Task<string> NextSalesOrderNoAsync(CancellationToken cancellationToken = default)
    {
        var sequence = await dbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('\"OM_SalesOrderNoSeq\"') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return $"SO-{DateTimeOffset.UtcNow:yyyy}-{sequence:000000}";
    }
}
