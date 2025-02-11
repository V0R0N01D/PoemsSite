using Elastic.Clients.Elasticsearch;
using Poems.Common.Models.Configurations;
using Poems.Common.Models.Elasticsearch;
using Poems.Site.Interfaces.Repository;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Repositories;

public class ElasticPoemSearchRepository(
    ElasticsearchClient elasticsearchClient,
    ElasticsearchConfiguration elasticsearchConfiguration)
    : IPoemSearchRepository
{
    public async Task<SearchResultDto> SearchPoemsAsync(
        string query, int maxCount, float minimalRank,
        CancellationToken cancellationToken = default)
    {
        var fields = Fields.FromFields([
            new Field("author", 8),
            new Field("title", 5),
            new Field("text", 1.5)
        ]);

        var searchResponse = await elasticsearchClient.SearchAsync<PoemWithAuthor>(search => search
                .Index(elasticsearchConfiguration.IndexTitle)
                .SourceExcludes(Fields.FromString("text"))
                .Query(qd => qd
                    .MultiMatch(mmq => mmq
                        .Query(query)
                        .Fields(fields)
                    )
                )
                .MinScore(minimalRank)
                .Size(maxCount),
            cancellationToken);

        if (!searchResponse.IsValidResponse)
        {
            throw searchResponse.ApiCallDetails.OriginalException
                  ?? new Exception(
                      $"Elasticsearch request failed: {searchResponse.DebugInformation}.");
        }

        var poems = searchResponse.Hits
            .Where(hit => hit.Score >= minimalRank)
            .Select(hit => new PoemShortDto
            {
                Id = hit.Source!.Id,
                AuthorName = hit.Source.AuthorName,
                Title = hit.Source.Title,
                Rank = hit.Score ?? 0
            })
            .ToList();

        return new SearchResultDto
        {
            Poems = poems,
            QueryTimeMs = searchResponse.Took
        };
    }
}