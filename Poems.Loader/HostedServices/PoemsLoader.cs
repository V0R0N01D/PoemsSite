using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Poems.Common.Models.Elasticsearch;
using Poems.Loader.Services.Interfaces;
using Poems.Loader.Models;

namespace Poems.Loader.HostedServices;

/// <summary>
/// Background service for loading poems into the database.
/// </summary>
public class PoemsLoader : BackgroundService
{
    /// <summary>
    /// The size of each chunk when processing records.
    /// </summary>
    private const int ChunkSize = 10000;

    private readonly ILogger<PoemsLoader> _logger;
    private readonly IFileInfoProvider _fileInfoProvider;
    private readonly IFileReader<PoemRecord> _fileReaderService;
    private readonly IDataImporter<PoemRecord> _dataImporter;
    private readonly IImportPreparationService _elasticsearchImportPreparationService;
    private readonly IRecordsReader<PoemWithAuthor> _dbReaderService;
    private readonly IDataImporter<PoemWithAuthor> _elasticsearchImporter;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoemsLoader"/> class.
    /// </summary>
    /// <param name="logger">Logger for logging information.</param>
    /// <param name="fileInfoProvider">Service to provide file information.</param>
    /// <param name="fileReaderService">Service to read poem records from a file.</param>
    /// <param name="dataImporter">Service to import poem records into the database.</param>
    /// <param name="dbReaderService"></param>
    /// <param name="elasticsearchImporter"></param>
    /// <param name="elasticsearchImportPreparationService"></param>
    public PoemsLoader(ILogger<PoemsLoader> logger,
        IFileReader<PoemRecord> fileReaderService,
        IFileInfoProvider fileInfoProvider,
        IDataImporter<PoemRecord> dataImporter,
        IRecordsReader<PoemWithAuthor> dbReaderService,
        IDataImporter<PoemWithAuthor> elasticsearchImporter,
        IImportPreparationService elasticsearchImportPreparationService
    )
    {
        _logger = logger;
        _fileInfoProvider = fileInfoProvider;
        _fileReaderService = fileReaderService;
        _dataImporter = dataImporter;
        _dbReaderService = dbReaderService;
        _elasticsearchImporter = elasticsearchImporter;
        _elasticsearchImportPreparationService = elasticsearchImportPreparationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var fileInfo = _fileInfoProvider.GetFileInfo();
        var poemRecords = _fileReaderService.ReadRecords(fileInfo);
        
        var dbImportedRecordsCount = 0;
        foreach (var partOfRecords in poemRecords.Chunk(ChunkSize))
        {
            await _dataImporter.ImportRecordsAsync(partOfRecords, stoppingToken);
            _logger.LogInformation($"Импортировано {partOfRecords.Length} записей в бд.");
            dbImportedRecordsCount += partOfRecords.Length;
        }
        
        _logger.LogInformation($"Всего импортировано записей в бд: {dbImportedRecordsCount}.");

        
        await _elasticsearchImportPreparationService.PrepareDestinationAsync();
        var elasticImportedRecordsCount = 0;
        while (true)
        {
            var poems = await _dbReaderService.ReadRecordsAsync(
                ChunkSize,
                elasticImportedRecordsCount,
                stoppingToken);
        
            if (poems.Count == 0)
                break;
        
            await _elasticsearchImporter.ImportRecordsAsync(poems, stoppingToken);
            _logger.LogInformation($"Импортировано {poems.Count} записей в Elasticsearch.");
            elasticImportedRecordsCount += poems.Count;
        }

        _logger.LogInformation($"Всего импортировано записей в Elasticsearch: {elasticImportedRecordsCount}.");

        _logger.LogInformation("Для выключения нажмите Ctrl+C.");
    }
}