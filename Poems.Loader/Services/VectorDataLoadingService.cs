using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Poems.Common.Embeddings;
using Poems.Common.Models.Database;
using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.Services;

/// <summary>
/// Builds the initial vector index. Dataset/model updates require a fresh database.
/// </summary>
public class VectorDataLoadingService(
    PoemsContext context, PoemEmbeddingClient embeddingClient, ILogger<VectorDataLoadingService> logger)
    : IDataLoadingService
{
    public async Task LoadDataAsync(CancellationToken stoppingToken)
    {
        // Release entities tracked during the preceding CSV import.
        context.ChangeTracker.Clear();
        if (await context.PoemChunks.AnyAsync(stoppingToken))
        {
            logger.LogInformation("Векторный индекс уже заполнен. Для обновления пересоздайте БД.");
            return;
        }

        // Publish the whole index atomically: an interrupted run cannot look like a complete import.
        await using var transaction = await context.Database.BeginTransactionAsync(stoppingToken);

        var lastPoemId = 0;
        var poemCount = 0;
        var chunkCount = 0;

        logger.LogInformation("Первичная индексация стихов моделью {Model}. Это может занять длительное время.",
            PoemEmbeddingClient.ModelVersion);

        while (true)
        {
            var poems = await context.Poems.AsNoTracking()
                .Where(poem => poem.Id > lastPoemId).OrderBy(poem => poem.Id).Take(64)
                .Select(poem => new { poem.Id, poem.Title, poem.Content, Author = poem.Author.Name })
                .ToArrayAsync(stoppingToken);
            if (poems.Length == 0) break;

            foreach (var poem in poems)
            {
                var chunks = await embeddingClient.EmbedPoemAsync(
                    poem.Author, poem.Title, poem.Content, stoppingToken);
                context.PoemChunks.AddRange(chunks.Select((chunk, number) => new PoemChunk
                {
                    PoemId = poem.Id,
                    ChunkNumber = number,
                    Text = chunk.Text,
                    Embedding = new Vector(chunk.Embedding)
                }));
                chunkCount += chunks.Count;
            }

            await context.SaveChangesAsync(stoppingToken);
            context.ChangeTracker.Clear();
            lastPoemId = poems[^1].Id;
            poemCount += poems.Length;

            logger.LogInformation($"Обработано стихов: {poemCount}; фрагментов: {chunkCount}.");
        }

        await transaction.CommitAsync(stoppingToken);

        // Refresh planner statistics after the bulk load.
        await context.Database.ExecuteSqlRawAsync("ANALYZE poem_chunks", stoppingToken);

        logger.LogInformation($"Векторный индекс готов: {poemCount} стихов, {chunkCount} фрагментов.");
    }
}
