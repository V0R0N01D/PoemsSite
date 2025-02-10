using Microsoft.EntityFrameworkCore;
using Poems.Common.Models.Database;
using Poems.Site.Interfaces.Repository;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Repositories;

public class PoemRepository(PoemsContext dbContext) : IPoemRepository
{
    public async Task<PoemFullDto?> GetPoemByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Poems.Where(poem => poem.Id == id)
            .Select(poem => new PoemFullDto
            {
                Id = id,
                Title = poem.Title,
                AuthorName = poem.Author.Name,
                Content = poem.Content
            }).FirstOrDefaultAsync(cancellationToken);
    }
}