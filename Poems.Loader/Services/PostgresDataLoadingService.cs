using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Poems.Loader.Models;
using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.Services;

public class PostgresDataLoadingService(
    ILogger<PostgresDataLoadingService> logger,
    [FromKeyedServices("postgres")] IImportPreparationService importPreparationService,
    IFileReader<PoemRecord> fileReaderService,
    IFileInfoProvider fileInfoProvider,
    IDataImporter<PoemRecord> dataImporter)
    : IDataLoadingService
{
    private const int ChunkSize = 10000;

    public async Task LoadDataAsync(CancellationToken stoppingToken)
    {
        var serviceReady = await importPreparationService.PrepareDestinationAsync(stoppingToken);
        if (!serviceReady) return;

        var fileInfo = fileInfoProvider.GetFileInfo();
        var poemRecords = fileReaderService.ReadRecords(fileInfo);

        var dbImportedRecordsCount = 0;
        foreach (var partOfRecords in poemRecords.Chunk(ChunkSize))
        {
            await dataImporter.ImportRecordsAsync(partOfRecords, stoppingToken);
            logger.LogInformation($"Импортировано {partOfRecords.Length} записей в бд.");
            dbImportedRecordsCount += partOfRecords.Length;
        }

        logger.LogInformation($"Всего импортировано записей в бд: {dbImportedRecordsCount}.");
    }
}