using Poems.Site.Interfaces.Repository;
using Poems.Site.Interfaces.Services;
using Poems.Site.Models;
using Poems.Site.Models.Dtos;
using Poems.Common.Embeddings;

namespace Poems.Site.Services;

internal class PoemService(
    IPoemRepository poemRepository,
    [FromKeyedServices("postgre")] IPoemSearchRepository postgrePoemSearchRepository,
    [FromKeyedServices("elastic")] IPoemSearchRepository elasticPoemSearchRepository,
    [FromKeyedServices("vector")] IPoemSearchRepository? vectorPoemSearchRepository,
    ILogger<PoemService> logger)
    : IPoemService
{
    public async Task<Result<PoemFullDto>> GetPoemByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        var poem = await poemRepository.GetPoemByIdAsync(id, cancellationToken);

        return poem is null
            ? Result<PoemFullDto>.Failure("Poem not found.", 404)
            : Result<PoemFullDto>.Success(poem);
    }

    public async Task<Result<SearchResultDto>> DbSearchPoemsAsync(
        string query, int maxCount = 30, float minimalRank = 0.3F,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Result<SearchResultDto>.Failure("Poem query is empty.");

        if (maxCount < 1)
            maxCount = 30;

        if (minimalRank < 0 || minimalRank > 1)
            minimalRank = 0.3F;

        var poems = await postgrePoemSearchRepository
            .SearchPoemsAsync(query, maxCount, minimalRank, cancellationToken);

        return Result<SearchResultDto>.Success(poems);
    }

    public async Task<Result<SearchResultDto>> ElasticSearchPoemsAsync(
        string query, int maxCount = 30, float minimalRank = 2.3F,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Result<SearchResultDto>.Failure("Poem query is empty.");

        if (maxCount < 1)
            maxCount = 30;

        if (minimalRank < 0)
            minimalRank = 2.3F;

        var poems = await elasticPoemSearchRepository
            .SearchPoemsAsync(query, maxCount, minimalRank, cancellationToken);

        return Result<SearchResultDto>.Success(poems);
    }

    public async Task<Result<SearchResultDto>> VectorSearchPoemsAsync(
        string query, CancellationToken cancellationToken = default)
    {
        if (vectorPoemSearchRepository is null)
            return Result<SearchResultDto>.Failure("Поиск по смыслу отключён.", 503);

        query = query.Trim();
        if (query.Length < 3 || query.Length > PoemEmbeddingClient.MaxQueryCharacters)
            return Result<SearchResultDto>.Failure("Введите от 3 до 1000 символов для поиска по смыслу.");

        try
        {
            var poems = await vectorPoemSearchRepository.SearchPoemsAsync(query, 30, 0, cancellationToken);
            return Result<SearchResultDto>.Success(poems);
        }
        catch (EmbeddingQueryException exception)
        {
            return Result<SearchResultDto>.Failure(exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<SearchResultDto>.Failure("Поиск по смыслу занял слишком много времени. Попробуйте ещё раз.", 503);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException or System.Text.Json.JsonException)
        {
            logger.LogWarning(exception, "Сервис эмбеддингов недоступен или вернул некорректный ответ.");
            return Result<SearchResultDto>.Failure("Поиск по смыслу временно недоступен. Попробуйте ещё раз.", 503);
        }
    }
}
