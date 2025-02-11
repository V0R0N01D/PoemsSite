using Microsoft.EntityFrameworkCore;
using Poems.Common.Models.Database;
using Poems.Site.Interfaces.Repository;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Repositories;

public class PostgrePoemSearchRepository(
    PoemsContext dbContext,
    IHttpContextAccessor httpContextAccessor)
    : IPoemSearchRepository
{
    public async Task<SearchResultDto> SearchPoemsAsync(
        string query, int maxCount, float minimalRank,
        CancellationToken cancellationToken)
    {
        var poems = await dbContext.Poems
            .Where(poem => poem.Searchvector!.Matches(
                EF.Functions.PlainToTsQuery("russian", query)))
            .Select(poem => new
            {
                Id = poem.Id,
                Title = poem.Title,
                AuthorName = poem.Author.Name,
                Rank = poem.Searchvector!.Rank(
                    EF.Functions.PlainToTsQuery("russian", query))
            })
            .Where(poem => poem.Rank >= minimalRank)
            .OrderByDescending(poem => poem.Rank)
            .Take(maxCount)
            .Select(poem => new PoemShortDto
            {
                Id = poem.Id,
                AuthorName = poem.AuthorName,
                Title = poem.Title,
                Rank = poem.Rank
            })
            .ToArrayAsync(cancellationToken);

        var httpContext = httpContextAccessor.HttpContext;
        var queryTime = httpContext?.Items["DbQueryDuration"] as double? ?? 0;

        return new SearchResultDto
        {
            Poems = poems,
            QueryTimeMs = queryTime
        };
    }
}