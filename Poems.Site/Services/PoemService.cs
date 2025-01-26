using Poems.Site.Interfaces.IRepository;
using Poems.Site.Interfaces.IServices;
using Poems.Site.Models;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Services;

internal class PoemService : IPoemService
{
    private readonly IPoemRepository _poemRepository;

    public PoemService(IPoemRepository poemRepository)
    {
        _poemRepository = poemRepository;
    }

    public async Task<Result<PoemFullDto>> GetPoemByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        var poem = await _poemRepository.GetPoemByIdAsync(id, cancellationToken);

        return poem is null
            ? Result<PoemFullDto>.Failure("Poem not found.", 404)
            : Result<PoemFullDto>.Success(poem);
    }

    public async Task<Result<IEnumerable<PoemShortDto>>> SearchPoemsAsync(
        string query, int maxCount = 30, float minimalRank = 0.3F,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Result<IEnumerable<PoemShortDto>>.Failure("Poem query is empty.");

        if (maxCount < 1)
            maxCount = 30;

        if (minimalRank < 0 || minimalRank > 1)
            minimalRank = 0.3F;

        var poems = await _poemRepository
            .SearchPoemsAsync(query, maxCount, minimalRank, cancellationToken);

        return Result<IEnumerable<PoemShortDto>>.Success(poems!);
    }
}