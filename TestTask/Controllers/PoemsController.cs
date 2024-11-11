using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestTask.Models;

namespace TestTask.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PoemsController : ControllerBase
{
	private readonly TestTaskContext _dbContext;

	public PoemsController(TestTaskContext context)
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