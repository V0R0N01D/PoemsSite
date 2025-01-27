using System.Net;
using Microsoft.EntityFrameworkCore;
using Poems.Common.Database;
using Poems.Site.Infrastructure.Interceptors;
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

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDbContext<PoemsContext>((sp, options) =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("PoemsDatabase"));
            options.AddInterceptors(sp.GetRequiredService<QueryMetricsInterceptor>());
        });

        builder.Services.AddScoped<QueryMetricsInterceptor>();
        builder.Services.AddScoped<IPoemRepository, PoemRepository>();
        builder.Services.AddScoped<IPoemService, PoemService>();

        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Internal Server Error.");
            });
        });

        app.UseStaticFiles();
        app.UseRouting();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}