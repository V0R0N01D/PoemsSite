using Poems.Site.Models.Dtos;

namespace Poems.Site.Interfaces.Repository;

public interface IPoemSearchRepository
{
    Task<SearchResultDto> SearchPoemsAsync(string query, int maxCount,
        float minimalRank, CancellationToken cancellationToken = default);
}