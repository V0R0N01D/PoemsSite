using CsvHelper.Configuration.Attributes;

namespace Poems.Loader.Models;

/// <summary>
/// Data structure representing a poem record from a CSV file.
/// </summary>
public struct PoemRecord
{
	/// <summary>
	/// Name of the author.
	/// </summary>
	[Name("author")]
	public required string AuthorName { get; set; }

	/// <summary>
	/// Title of the poem.
	/// </summary>
	[Name("name")]
	public required string Title { get; set; }

	/// <summary>
	/// Text content of the poem.
	/// </summary>
	[Name("text")]
	public required string Text { get; set; }
}