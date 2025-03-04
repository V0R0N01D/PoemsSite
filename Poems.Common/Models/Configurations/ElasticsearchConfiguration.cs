namespace Poems.Common.Models.Configurations;

public class ElasticsearchConfiguration
{
    public required string Url { get; set; }
    public required string IndexTitle { get; set; }
    public required string Login { get; set; }
    public required string Password { get; set; }
}