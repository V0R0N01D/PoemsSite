using Microsoft.AspNetCore.Mvc;
using Poems.Site.Interfaces.IServices;
using Poems.Site.Models;
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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PoemFullDto>> GetPoemById(int id)
    {
        var poemResult = await _poemService.GetPoemByIdAsync(id);

        return poemResult.ToActionResult();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PoemShortDto>>> SearchPoems(
        [FromQuery] string query)
    {
        var poemsResult = await _poemService.SearchPoemsAsync(query);

        return poemsResult.ToActionResult();
    }
}