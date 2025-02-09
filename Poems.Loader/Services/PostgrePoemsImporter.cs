using Poems.Loader.Services.Interfaces;
using Poems.Loader.Models;
using Poems.Common.Models.Database;

namespace Poems.Loader.Services;

/// <summary>
/// Imports poem records into the database.
/// </summary>
public class PostgrePoemsImporter : IDataImporter<PoemRecord>
{
    private readonly Dictionary<string, Author> _cacheAuthors = new();
    private readonly PoemsContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgrePoemsImporter"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PostgrePoemsImporter(PoemsContext context) => _context = context;
    
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
                _context.Authors.Add(author);
            }

            var poem = new Poem()
            {
                Title = record.Title,
                Content = record.Text,
                Author = author
            };
            _context.Poems.Add(poem);
        }

        await _context.SaveChangesAsync(stoppingToken);
    }
}