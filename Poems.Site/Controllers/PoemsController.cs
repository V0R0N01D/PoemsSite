using Microsoft.AspNetCore.Mvc;
using Poems.Site.Interfaces.Services;
using Poems.Site.Models;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PoemsController(IPoemService poemService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PoemFullDto>> GetPoemById(int id)
    {
        var poemResult = await poemService.GetPoemByIdAsync(id);
        return poemResult.ToActionResult();
    }

    [HttpGet("search/db")]
    public async Task<ActionResult<SearchResultDto>> DbSearchPoems(
        [FromQuery] string query)
    {
        var poemsResult = await poemService.DbSearchPoemsAsync(query);
        return poemsResult.ToActionResult();
    }
    
    [HttpGet("search/elastic")]
    public async Task<ActionResult<SearchResultDto>> ElasticSearchPoems(
        [FromQuery] string query)
    {
        var poemsResult = await poemService.ElasticSearchPoemsAsync(query);
        return poemsResult.ToActionResult();
    }

    [HttpGet("search/vector")]
    public async Task<ActionResult<SearchResultDto>> VectorSearchPoems(
        [FromQuery] string query, CancellationToken cancellationToken)
    {
        var poemsResult = await poemService.VectorSearchPoemsAsync(query, cancellationToken);
        return poemsResult.ToActionResult();
    }
}
