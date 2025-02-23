using Elastic.Clients.Elasticsearch;
using Poems.Common.Models.Configurations;
using Poems.Common.Models.Elasticsearch;
using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.Services;

public class ElasticsearchImportPreparationService(
    ElasticsearchClient elasticsearchClient,
    ElasticsearchConfiguration elasticsearchConfiguration)
    : IImportPreparationService
{
    public async Task<bool> PrepareDestinationAsync(CancellationToken cancellationToken = default)
    {
        var indexTitle = elasticsearchConfiguration.IndexTitle;
        await DeleteIndex(indexTitle);
        await CreateIndex(indexTitle);
        return true;
    }

    private async Task CreateIndex(string title)
    {
        await elasticsearchClient.Indices.CreateAsync<PoemWithAuthor>(index => index
            .Index(title)
            .Mappings(mappings => mappings
                .Properties(properties => properties
                    .Keyword(poem => poem.Id,
                        keyword => keyword.Index(false))
                    .Text(poem => poem.AuthorName,
                        conf => conf
                            .Analyzer("russian"))
                    .Text(poem => poem.Title,
                        conf => conf
                            .Analyzer("russian"))
                    .Text(poem => poem.Text,
                        conf => conf
                            .Analyzer("russian"))
                )
            ));
    }

    private async Task DeleteIndex(string title)
    {
        await elasticsearchClient.Indices.DeleteAsync(title);
    }
}