using Poems.Loader.Services.Interfaces;
using Poems.Loader.Models;
using Poems.Common.Models.Database;

namespace Poems.Loader.Services;

/// <summary>
/// Imports poem records into the database.
/// </summary>
public class PostgrePoemsImporter(PoemsContext context) : IDataImporter<PoemRecord>
{
    private readonly Dictionary<string, Author> _cacheAuthors = new();
    
    /// <inheritdoc/>
    public async Task ImportRecordsAsync(IEnumerable<PoemRecord> records,
        CancellationToken stoppingToken = default)
    {
        foreach (var record in records)
        {
            if (!_cacheAuthors.TryGetValue(record.AuthorName, out var author))
            {
                author = new Author()
                {
                    Name = record.AuthorName,
                };

                _cacheAuthors.Add(author.Name, author);
                context.Authors.Add(author);
            }

            var poem = new Poem()
            {
                Title = record.Title,
                Content = record.Text,
                Author = author
            };
            context.Poems.Add(poem);
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}