using Microsoft.AspNetCore.Mvc;
using Poems.Site.Views.Home;

namespace Poems.Site.Controllers;

public class HomeController : Controller
{
    public IActionResult Index([FromServices] IConfiguration configuration)
    {
        var model = new IndexModel(configuration);
        return View(model);
    }
}