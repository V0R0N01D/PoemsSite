using CsvHelper;
using CsvHelper.Configuration;
using Poems.Loader.Services.Interfaces;
using Poems.Loader.Models;
using System.Globalization;

namespace Poems.Loader.Services;

/// <summary>
/// Lazy reads poem records from a CSV file.
/// </summary>
public class CsvLazyFileReader : IFileReader<PoemRecord>
{
	public IEnumerable<PoemRecord> ReadRecords(FileInfo file)
	{
		using var reader = new StreamReader(file.FullName);
		var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			HasHeaderRecord = true,
		};

		using var csvReader = new CsvReader(reader, csvConfig);
		while (csvReader.Read())
		{
			yield return csvReader.GetRecord<PoemRecord>();
		}
	}
}
