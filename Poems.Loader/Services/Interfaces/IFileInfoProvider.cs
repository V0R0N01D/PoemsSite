namespace Poems.Loader.Services.Interfaces;

/// <summary>
/// Interface for providing file information.
/// </summary>
public interface IFileInfoProvider
{
	/// <summary>
	/// Gets the file information.
	/// </summary>
	/// <returns>The <see cref="FileInfo"/> object representing the file.</returns>
	public FileInfo GetFileInfo();
}
