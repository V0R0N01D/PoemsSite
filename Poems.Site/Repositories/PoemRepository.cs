using Microsoft.EntityFrameworkCore;
using Poems.Common.Database;
using Poems.Site.Interfaces.IRepository;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Repositories;

public class PoemRepository : IPoemRepository
{
	private readonly PoemsContext _dbContext;

	public PoemRepository(PoemsContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<IEnumerable<PoemShortDto>> SearchPoemsAsync(string query, int maxCount,
		float minimalRank, CancellationToken cancellationToken)
	{
		return await _dbContext.Poems
			.Where(poem =>
				poem.Searchvector!.Matches(EF.Functions.PlainToTsQuery("russian", query)))
			.Select(poem => new
			{
				Id = poem.Id,
				Title = poem.Title,
				AuthorName = poem.Author.Name,
				Rank = poem.Searchvector!.Rank(EF.Functions.PlainToTsQuery("russian", query))
			})
			.Where(poem => poem.Rank >= minimalRank)
			.OrderByDescending(poem => poem.Rank)
			.Take(maxCount)
			.Select(poem => new PoemShortDto
			{
				Id = poem.Id,
				AuthorName = poem.AuthorName,
				Title = poem.Title,
			})
			.ToArrayAsync(cancellationToken);
	}
}
