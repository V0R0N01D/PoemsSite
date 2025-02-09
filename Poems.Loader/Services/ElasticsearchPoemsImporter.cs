using Elastic.Clients.Elasticsearch;
using Poems.Common.Models.Elasticsearch;
using Poems.Loader.Models;
using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.Services;

public class ElasticsearchPoemsImporter(ElasticsearchClient elasticsearchClient)
    : IDataImporter<PoemWithAuthor>
{
    public async Task ImportRecordsAsync(IEnumerable<PoemWithAuthor> records,
        CancellationToken stoppingToken = default)
    {
        await elasticsearchClient
            .BulkAsync(b => b
                    .Index("poems")
                    .CreateMany(records),
                stoppingToken);
    }
}