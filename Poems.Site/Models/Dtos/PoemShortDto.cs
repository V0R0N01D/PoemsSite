using System.Text.Json.Serialization;

namespace Poems.Site.Models.Dtos;

public class PoemShortDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("author_name")]
	public required string AuthorName { get; set; }

	[JsonPropertyName("title")]
	public required string Title { get; set; }
	
	[JsonPropertyName("rank")]
	public required double Rank { get; set; }
	
	
}