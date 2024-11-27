using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.Services;

/// <summary>
/// Provides file information by prompting the user via the console.
/// </summary>
internal class ConsoleFileInfoProvider : IFileInfoProvider
{
	public FileInfo GetFileInfo()
	{
		while (true)
		{
			Console.WriteLine("Введите путь к csv файлу:");

			var filePath = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(filePath))
			{
				Console.WriteLine("Путь указан неверно.");
				continue;
			}

			var fileInfo = new FileInfo(filePath);

			if (fileInfo.Exists && fileInfo.Extension == ".csv")
				return fileInfo;

			Console.WriteLine("Файл не существует или имеет неверное расширение." + Environment.NewLine);
		}
	}
}
