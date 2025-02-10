using Poems.Site.Models.Dtos;

namespace Poems.Site.Interfaces.Repository;

public interface IPoemRepository
{
    Task<PoemFullDto?> GetPoemByIdAsync(int id,
        CancellationToken cancellationToken = default);
}