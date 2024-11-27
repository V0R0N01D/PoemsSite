using Microsoft.EntityFrameworkCore;
using Poems.Common.Database;

namespace Poems.Site;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Configuration.AddJsonFile("connection_strings.json");

		builder.Services.AddDbContext<PoemsContext>(options =>
			options.UseNpgsql(builder.Configuration.GetConnectionString("PoemsDatabase")));

		builder.Services.AddControllersWithViews();

		var app = builder.Build();

		app.UseStaticFiles();

		app.UseRouting();

		app.MapControllerRoute(
			name: "default",
			pattern: "{controller=Home}/{action=Index}/{id?}");

		app.Run();
	}
}
