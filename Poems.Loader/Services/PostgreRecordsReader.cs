using Microsoft.EntityFrameworkCore;
using Poems.Common.Models.Database;
using Poems.Common.Models.Elasticsearch;
using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.Services;

public class PostgreRecordsReader(PoemsContext context) : IRecordsReader<PoemWithAuthor>
{
    public async Task<IList<PoemWithAuthor>> ReadRecordsAsync(int maxCount, int startIndex = 0,
        CancellationToken cancellationToken = default)
    {
        return await context.Poems
            .OrderBy(poem => poem.Id)
            .Skip(startIndex)
            .Take(maxCount)
            .Select(poem => new PoemWithAuthor
            {
                Id = poem.Id,
                Title = poem.Title,
                Text = poem.Content
                    .Replace("\r\n", " ")
                    .Replace("\r", " ")
                    .Replace("\n", " "),
                AuthorName = poem.Author.Name
            }).ToArrayAsync(cancellationToken);
    }
}