using System.Text.Json.Serialization;

namespace Poems.Common.Models.Elasticsearch;

/// <summary>
/// Data class representing a poem with author.
/// </summary>
public class PoemWithAuthor
{
    /// <summary>
    /// Identify of the poem.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Name of the author.
    /// </summary>
    [JsonPropertyName("author")]
    public required string AuthorName { get; set; }

    /// <summary>
    /// Title of the poem.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Text content of the poem.
    /// </summary>
    public required string Text { get; set; }
}