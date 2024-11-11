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
			.Select(poem => $"{poem.Title} - {poem.Author.Name}")
			.ToListAsync();

		return result;
	}
}