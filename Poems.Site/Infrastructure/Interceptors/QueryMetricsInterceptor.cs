using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Poems.Site.Infrastructure.Interceptors;

public class QueryMetricsInterceptor(IHttpContextAccessor httpContextAccessor)
    : DbCommandInterceptor
{
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext != null)
        {
            var elapsed = eventData.Duration.TotalMilliseconds;
            httpContextAccessor.HttpContext.Items["DbQueryDuration"] = elapsed;
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}