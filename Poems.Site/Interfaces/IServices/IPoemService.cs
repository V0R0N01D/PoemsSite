using Poems.Site.Models;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Interfaces.IServices;

public interface IPoemService
{
    Task<Result<IEnumerable<PoemShortDto>>> SearchPoemsAsync(string text, int maxCount = 30,
        float minimalRank = 0.3F, CancellationToken cancellationToken = default);

    Task<Result<PoemFullDto>> GetPoemByIdAsync(int id,
        CancellationToken cancellationToken = default);
}