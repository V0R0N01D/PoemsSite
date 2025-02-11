using System.Net;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Poems.Common.Models.Configurations;
using Poems.Common.Models.Database;
using Poems.Site.Infrastructure.Interceptors;
using Poems.Site.Interfaces.Repository;
using Poems.Site.Interfaces.Services;
using Poems.Site.Repositories;
using Poems.Site.Services;

namespace Poems.Site;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("connection_strings.json");

        #region Postgre

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDbContext<PoemsContext>((sp, options) =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("PoemsDatabase"));
            options.AddInterceptors(sp.GetRequiredService<QueryMetricsInterceptor>());
        });
        builder.Services.AddScoped<QueryMetricsInterceptor>();

        #endregion

        #region Elasticsearch

        var elasticConfiguration = builder.Configuration
            .GetRequiredSection("ExternalServices")
            .GetRequiredSection("ElasticSearch")
            .Get<ElasticsearchConfiguration>()!;
        builder.Services.AddSingleton(elasticConfiguration);

        builder.Services.AddSingleton<ElasticsearchClient>(_
            => new ElasticsearchClient(new ElasticsearchClientSettings(
                    new Uri(elasticConfiguration.Url))
                .CertificateFingerprint(elasticConfiguration.Fingerprint)
                .Authentication(new ApiKey(elasticConfiguration.ApiKey))));

        #endregion


        builder.Services
            .AddKeyedScoped<IPoemSearchRepository, PostgrePoemSearchRepository>("postgre");
        builder.Services
            .AddKeyedScoped<IPoemSearchRepository, ElasticPoemSearchRepository>("elastic");
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