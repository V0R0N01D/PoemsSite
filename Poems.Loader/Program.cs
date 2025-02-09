using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Poems.Common.Models.Configurations;
using Poems.Common.Models.Database;
using Poems.Common.Models.Elasticsearch;
using Poems.Loader.Services.Interfaces;
using Poems.Loader.Models;
using Poems.Loader.Services;
using Poems.Loader.HostedServices;

namespace Poems.Loader;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.AddJsonFile("connection_strings.json");

        #region Postgre

        builder.Services.AddDbContext<PoemsContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("PoemsDatabase")));

        builder.Services.AddScoped<IFileInfoProvider, ConfigurationFileInfoProvider>(provider =>
            new ConfigurationFileInfoProvider(provider.GetRequiredService<IConfiguration>()));
        builder.Services.AddScoped<IFileReader<PoemRecord>, CsvLazyFileReader>();
        builder.Services.AddScoped<IDataImporter<PoemRecord>, PostgrePoemsImporter>();

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

        builder.Services.AddScoped<IImportPreparationService, ElasticsearchImportPreparationService>();
        builder.Services.AddScoped<IRecordsReader<PoemWithAuthor>, PostgreRecordsReader>();
        builder.Services.AddScoped<IDataImporter<PoemWithAuthor>, ElasticsearchPoemsImporter>();

        #endregion

        builder.Services.AddHostedService<PoemsLoader>();

        var host = builder.Build();
        await host.RunAsync();
    }
}