using Microsoft.AspNetCore.Mvc;
using Poems.Site.Interfaces.IRepository;
using Poems.Site.Interfaces.IServices;
using Poems.Site.Models;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Services;

internal class PoemService : IPoemService
{
    private readonly IPoemRepository _poemRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PoemService(IPoemRepository poemRepository, IHttpContextAccessor httpContextAccessor)
    {
        _poemRepository = poemRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<PoemFullDto>> GetPoemByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        var poem = await _poemRepository.GetPoemByIdAsync(id, cancellationToken);

        return poem is null
            ? Result<PoemFullDto>.Failure("Poem not found.", 404)
            : Result<PoemFullDto>.Success(poem);
    }

    public async Task<Result<SearchResultDto>> SearchPoemsAsync(
        string query, int maxCount = 30, float minimalRank = 0.3F,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Result<SearchResultDto>.Failure("Poem query is empty.");

        if (maxCount < 1)
            maxCount = 30;

        if (minimalRank < 0 || minimalRank > 1)
            minimalRank = 0.3F;

        var poems = await _poemRepository
            .SearchPoemsAsync(query, maxCount, minimalRank, cancellationToken);

        var httpContext = _httpContextAccessor.HttpContext;
        var queryTime = httpContext?.Items["DbQueryDuration"] as double? ?? 0;

        return Result<SearchResultDto>.Success(new SearchResultDto()
        {
            Poems = poems,
            QueryTimeMs = queryTime
        });
    }
}