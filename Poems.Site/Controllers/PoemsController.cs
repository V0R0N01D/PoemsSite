using Microsoft.AspNetCore.Mvc;
using Poems.Site.Interfaces.IServices;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PoemsController : ControllerBase
{
	private readonly IPoemService _poemService;

	public PoemsController(IPoemService poemService)
	{
		_poemService = poemService;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<PoemShortDto>>> SearchPoems([FromQuery] string query)
	{
		var poems = await _poemService.SearchPoemsAsync(query);

		return Ok(poems);
	}
}