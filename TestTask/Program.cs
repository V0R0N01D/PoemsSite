using Microsoft.EntityFrameworkCore;
using TestTask.Models;

namespace TestTask;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("connection_strings.json");

        builder.Services.AddDbContext<TestTaskContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("TestTaskDatabase")));

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
