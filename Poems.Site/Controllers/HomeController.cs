using Microsoft.AspNetCore.Mvc;

namespace Poems.Site.Controllers;

public class HomeController : Controller
{
	public IActionResult Index()
	{
		return View();
	}
}
