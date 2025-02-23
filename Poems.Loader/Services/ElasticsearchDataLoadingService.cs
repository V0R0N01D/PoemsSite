using Microsoft.Extensions.Logging;
using Poems.Common.Models.Elasticsearch;
using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.Services;

public class ElasticsearchDataLoadingService(
    ILogger<ElasticsearchDataLoadingService> logger,
    IImportPreparationService elasticsearchImportPreparationService,
    IRecordsReader<PoemWithAuthor> dbReaderService,
    IDataImporter<PoemWithAuthor> elasticsearchImporter)
    : IDataLoadingService
{
    private const int ChunkSize = 5000;

    public async Task LoadDataAsync(CancellationToken stoppingToken)
    {
        var serviceReady = await elasticsearchImportPreparationService.PrepareDestinationAsync(stoppingToken);
        if (!serviceReady) return;
        
        var elasticImportedRecordsCount = 0;
        
        while (true)
        {
            var poems = await dbReaderService.ReadRecordsAsync(
                ChunkSize,
                elasticImportedRecordsCount,
                stoppingToken);

            if (poems.Count == 0)
                break;

            await elasticsearchImporter.ImportRecordsAsync(poems, stoppingToken);
            logger.LogInformation($"Импортировано {poems.Count} записей в Elasticsearch.");
            elasticImportedRecordsCount += poems.Count;
        }

        logger.LogInformation($"Всего импортировано записей в Elasticsearch: {elasticImportedRecordsCount}.");
    }
}