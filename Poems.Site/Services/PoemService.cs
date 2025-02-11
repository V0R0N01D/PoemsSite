using Poems.Site.Interfaces.Repository;
using Poems.Site.Interfaces.Services;
using Poems.Site.Models;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Services;

internal class PoemService(
    IPoemRepository poemRepository,
    [FromKeyedServices("postgre")] IPoemSearchRepository postgrePoemSearchRepository,
    [FromKeyedServices("elastic")] IPoemSearchRepository elasticPoemSearchRepository)
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
}