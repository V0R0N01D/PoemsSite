using System.Text.Json.Serialization;

namespace Poems.Site.Models.Dtos;

public class PoemFullDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("authorName")]
	public required string AuthorName { get; set; }

	[JsonPropertyName("title")]
	public required string Title { get; set; }

	[JsonPropertyName("content")]
	public required string Content { get; set; }
}
