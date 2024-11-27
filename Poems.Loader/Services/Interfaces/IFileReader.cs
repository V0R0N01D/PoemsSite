namespace Poems.Loader.Services.Interfaces;

/// <summary>
/// Interface for reading records from a file.
/// </summary>
public interface IFileReader<T>
{
	/// <summary>
	/// Reads records from the specified file.
	/// </summary>
	/// <param name="file">The file to read records from.</param>
	/// <returns>An enumerable collection of records.</returns>
	IEnumerable<T> ReadRecords(FileInfo file);
}
