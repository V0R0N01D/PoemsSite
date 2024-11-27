using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poems.Common.Database;

namespace Poems.Site.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PoemsController : ControllerBase
{
	private readonly PoemsContext _dbContext;

	public PoemsController(PoemsContext context)
	{
		_dbContext = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<string>>> SearchTitleWithAuthor([FromQuery] string query)
	{
		if (string.IsNullOrWhiteSpace(query))
			return Ok(new List<string>());

		var result = await _dbContext.Poems
			.Where(poem =>
				poem.Searchvector!.Matches(EF.Functions.PlainToTsQuery("russian", query)))
			.Select(poem => new
			{
				title = $"{poem.Title} - {poem.Author.Name}",
				rank = poem.Searchvector!.Rank(EF.Functions.PlainToTsQuery("russian", query))
			})
			.Where(poem => poem.rank >= 0.3)
			.OrderByDescending(poem => poem.rank)
			.Select(poem => poem.title)
			.ToListAsync();

		return result;
	}
}