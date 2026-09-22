using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Poems.Common.Embeddings;
using Poems.Common.Models.Database;
using Poems.Site.Interfaces.Repository;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Repositories;

public class VectorPoemSearchRepository(PoemsContext context, PoemEmbeddingClient embeddingClient)
    : IPoemSearchRepository
{
    public async Task<SearchResultDto> SearchPoemsAsync(
        string query, int maxCount, float minimalRank, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var token = deadline.Token;
        var embedding = await embeddingClient.EmbedQueryAsync(query, token);
        maxCount = Math.Clamp(maxCount, 1, 30);

        await using var transaction = await context.Database.BeginTransactionAsync(token);

        // Oversample fragments before deduplicating poems; the setting stays local to this request.
        await context.Database.ExecuteSqlRawAsync("SET LOCAL hnsw.ef_search = 300", token);

        var poems = await context.Database.SqlQuery<PoemShortDto>($"""
            WITH candidates AS MATERIALIZED (
                SELECT poem_id, embedding <=> {embedding} AS distance
                FROM poem_chunks
                ORDER BY embedding <=> {embedding}
                LIMIT 300
            )
            SELECT p.id AS "Id", p.title AS "Title", a.name AS "AuthorName",
                   1.0 - min(c.distance) AS "Rank"
            FROM candidates c
            JOIN poems p ON p.id = c.poem_id
            JOIN authors a ON a.id = p.author_id
            GROUP BY p.id, p.title, a.name
            ORDER BY min(c.distance), p.id
            LIMIT {maxCount}
            """).ToArrayAsync(token);

        await transaction.CommitAsync(token);

        return new SearchResultDto
        {
            Poems = poems,
            QueryTimeMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds
        };
    }
}
