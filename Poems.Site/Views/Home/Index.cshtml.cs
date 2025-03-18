using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Poems.Site.Views.Home;

public class IndexModel : PageModel
{
    public bool ElasticsearchEnabled { get;  set; } = false;
    
    public IndexModel(IConfiguration configuration)
    {
        ElasticsearchEnabled = configuration.GetValue<bool>("ElasticsearchEnabled");
    }
}