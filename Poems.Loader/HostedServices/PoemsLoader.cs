using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
	private readonly IFileReader<PoemRecord> _readerService;
	private readonly IDataImporter<PoemRecord> _dataImporter;

	/// <summary>
	/// Initializes a new instance of the <see cref="PoemsLoader"/> class.
	/// </summary>
	/// <param name="logger">Logger for logging information.</param>
	/// <param name="fileInfoProvider">Service to provide file information.</param>
	/// <param name="readerService">Service to read poem records from a file.</param>
	/// <param name="dataImporter">Service to import poem records into the database.</param>
	public PoemsLoader(ILogger<PoemsLoader> logger, IFileInfoProvider fileInfoProvider,
		IFileReader<PoemRecord> readerService, IDataImporter<PoemRecord> dataImporter)
	{
		_logger = logger;
		_fileInfoProvider = fileInfoProvider;
		_readerService = readerService;
		_dataImporter = dataImporter;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var fileInfo = _fileInfoProvider.GetFileInfo();
		var poemRecords = _readerService.ReadRecords(fileInfo);

		int importedRecordsCount = 0;

		foreach (var partOfRecords in poemRecords.Chunk(ChunkSize))
		{
			await _dataImporter.ImportRecordsAsync(partOfRecords, stoppingToken);
			_logger.LogInformation($"Импортировано {partOfRecords.Length} записей.");
			importedRecordsCount += partOfRecords.Length;
		}

		_logger.LogInformation($"Всего импортировано записей: {importedRecordsCount}.");
		_logger.LogInformation("Для выключения нажмите Ctrl+C.");
	}
}
