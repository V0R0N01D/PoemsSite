namespace Poems.Loader.Services.Interfaces;

/// <summary>
/// Interface for importing data records.
/// </summary>
public interface IDataImporter<T>
{
	/// <summary>
	/// Imports a collection of records asynchronously.
	/// </summary>
	/// <param name="records">The records to import.</param>
	/// <param name="stoppingToken">Cancellation token to stop the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task ImportRecordsAsync(IEnumerable<T> records, CancellationToken stoppingToken);
}
