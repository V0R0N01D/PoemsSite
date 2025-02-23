using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Poems.Common.Models.Database;
using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.Services;

public class PostgresImportPreparationService(
    ILogger<PostgresImportPreparationService> logger,
    PoemsContext context) : IImportPreparationService
{
    public async Task<bool> PrepareDestinationAsync(CancellationToken cancellationToken = default)
    {
        return await CheckPoemExist(cancellationToken);
    }

    private async Task<bool> CheckPoemExist(CancellationToken cancellationToken = default)
    {
        var hasExistingRecords = await context.Poems.AnyAsync(cancellationToken);
        var message = hasExistingRecords
            ? "В базе данных уже есть записи стихов. Импорт в PostgreSQL пропускается."
            : "База данных пуста. Импорт в PostgreSQL будет выполнен.";
        logger.LogInformation(message);

        return !hasExistingRecords;
    }
}