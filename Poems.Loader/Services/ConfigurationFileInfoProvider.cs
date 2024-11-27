using Microsoft.Extensions.Configuration;
using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.Services;

/// <summary>
/// Provides file information based on configuration settings.
/// </summary>
public class ConfigurationFileInfoProvider : IFileInfoProvider
{
	private readonly string _filePath;

	/// <summary>
	/// Initializes a new instance of the <see cref="ConfigurationFileInfoProvider"/> class.
	/// </summary>
	/// <param name="configuration">App configuration.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ConfigurationFileInfoProvider(IConfiguration configuration)
	{
		var filePath = configuration["PathToFile"];

		if (string.IsNullOrWhiteSpace(filePath))
			throw new ArgumentNullException("PathToFile",
				"The PathToFile value is not specified in appsettings.json.");

		_filePath = filePath;
	}

	public FileInfo GetFileInfo()
	{
		var fileInfo = new FileInfo(_filePath);

		if (!fileInfo.Exists)
			throw new FileNotFoundException(fileInfo.FullName);

		if (fileInfo.Extension != ".csv")
			throw new FileLoadException($"{fileInfo.FullName} has an invalid extension.");

		return fileInfo;
	}
}
