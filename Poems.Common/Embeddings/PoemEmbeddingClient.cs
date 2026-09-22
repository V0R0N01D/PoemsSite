using System.Net;
using System.Net.Http.Json;
using Pgvector;

namespace Poems.Common.Embeddings;

public class PoemEmbeddingClient(HttpClient httpClient)
{
    public const string ModelVersion = "multilingual-e5-small-int8-v1";
    public const int Dimensions = 384;
    public const int MaxQueryCharacters = 1000;

    public async Task<Vector> EmbedQueryAsync(string query, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("embed/query", new { query }, cancellationToken);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            throw new EmbeddingQueryException("Сократите поисковый запрос: максимум 1000 символов и 256 токенов.");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<QueryEmbeddingResponse>(cancellationToken);
        ValidateVersion(result?.Version);

        return ToVector(result!.Embedding);
    }

    public async Task<IReadOnlyList<EmbeddedChunk>> EmbedPoemAsync(
        string author, string title, string content, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("embed/poem",
            new { author, title, content }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PoemEmbeddingResponse>(cancellationToken);
        ValidateVersion(result?.Version);

        if (result!.Chunks is not { Length: > 0 })
            throw new InvalidDataException("The embedding service returned no poem fragments.");

        foreach (var chunk in result.Chunks)
        {
            if (chunk.Text is null)
                throw new InvalidDataException("The embedding service returned an invalid fragment.");
            ToVector(chunk.Embedding);
        }

        return result.Chunks;
    }

    private static void ValidateVersion(string? version)
    {
        if (version != ModelVersion)
            throw new InvalidDataException("Embedding model version mismatch. Rebuild the services and poem index.");
    }

    private static Vector ToVector(float[]? values)
    {
        if (values is null || values.Length != Dimensions ||
            values.Any(value => !float.IsFinite(value)) || values.All(value => value == 0))
            throw new InvalidDataException("The embedding service returned an invalid vector.");
        return new Vector(values);
    }

    private sealed record QueryEmbeddingResponse(string Version, float[] Embedding);
    private sealed record PoemEmbeddingResponse(string Version, EmbeddedChunk[] Chunks);
}

public sealed record EmbeddedChunk(string Text, float[] Embedding);

public sealed class EmbeddingQueryException(string message) : Exception(message);
