using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Poems.Site.Infrastructure.Interceptors;

public class QueryMetricsInterceptor : DbCommandInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QueryMetricsInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            var elapsed = eventData.Duration.TotalMilliseconds;
            _httpContextAccessor.HttpContext.Items["DbQueryDuration"] = elapsed;
        }
        
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}