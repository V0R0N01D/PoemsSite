using Poems.Site.Models;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Interfaces.IRepository;

public interface IPoemRepository
{
    Task<IList<PoemShortDto>> SearchPoemsAsync(string query, int maxCount,
        float minimalRank, CancellationToken cancellationToken = default);

    Task<PoemFullDto?> GetPoemByIdAsync(int id,
        CancellationToken cancellationToken = default);
}