using System.Text.Json.Serialization;

namespace Poems.Site.Models.Dtos;

public class SearchResultDto
{
    [JsonPropertyName("poems")]
    public required IEnumerable<PoemShortDto> Poems { get; set; }

    [JsonPropertyName("queryTimeMs")]
    public double QueryTimeMs { get; set; }
}