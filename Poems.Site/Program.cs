using Microsoft.EntityFrameworkCore;
using Poems.Common.Database;
using Poems.Site.Interfaces.IRepository;
using Poems.Site.Interfaces.IServices;
using Poems.Site.Repositories;
using Poems.Site.Services;

namespace Poems.Site;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Configuration.AddJsonFile("connection_strings.json");

		builder.Services.AddDbContext<PoemsContext>(options =>
			options.UseNpgsql(builder.Configuration.GetConnectionString("PoemsDatabase")));

		builder.Services.AddScoped<IPoemRepository, PoemRepository>();
		builder.Services.AddScoped<IPoemService, PoemService>();

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
