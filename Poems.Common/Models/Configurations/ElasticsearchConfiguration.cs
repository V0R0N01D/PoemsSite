namespace Poems.Common.Models.Configurations;

public class ElasticsearchConfiguration
{
    public required string Url { get; set; }
    public required string Fingerprint { get; set; }
    public required string ApiKey { get; set; }
    public required string IndexTitle { get; set; }
}