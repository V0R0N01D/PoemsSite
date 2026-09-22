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
using Pgvector.EntityFrameworkCore;
using Poems.Common.Embeddings;

namespace Poems.Loader;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration
            .AddJsonFile("connection_strings.json", optional: true)
            .AddEnvironmentVariables();

        var elastisearchEnable = builder.Configuration.GetValue<bool>("ElasticsearchEnabled");

        #region Postgres

        builder.Services.AddDbContext<PoemsContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("PoemsDatabase"),
                postgres => postgres.UseVector().MigrationsAssembly(typeof(Program).Assembly.FullName)));

        builder.Services.AddScoped<IFileInfoProvider, ConfigurationFileInfoProvider>(provider =>
            new ConfigurationFileInfoProvider(provider.GetRequiredService<IConfiguration>()));
        builder.Services.AddScoped<IFileReader<PoemRecord>, CsvLazyFileReader>();
        builder.Services.AddScoped<IDataImporter<PoemRecord>, PostgrePoemsImporter>();
        builder.Services.AddKeyedScoped<IImportPreparationService, PostgresImportPreparationService>("postgres");
        builder.Services.AddKeyedScoped<IDataLoadingService, PostgresDataLoadingService>("postgres");

        #endregion

        if (builder.Configuration.GetValue<bool>("VectorSearchEnabled"))
        {
            builder.Services.AddPoemEmbeddings(builder.Configuration);
            builder.Services.AddKeyedScoped<IDataLoadingService, VectorDataLoadingService>("vector");
        }
        else
        {
            builder.Services.AddKeyedScoped<IDataLoadingService>("vector", (_, _) => null!);
        }

        #region Elasticsearch

        if (elastisearchEnable)
        {
            var elasticConfiguration = builder.Configuration
                .GetRequiredSection("ExternalServices")
                .GetRequiredSection("ElasticSearch")
                .Get<ElasticsearchConfiguration>()!;
            builder.Services.AddSingleton(elasticConfiguration);

            builder.Services.AddSingleton<ElasticsearchClient>(_
                => new ElasticsearchClient(new ElasticsearchClientSettings(
                        new Uri(elasticConfiguration.Url))
                    .ServerCertificateValidationCallback((_, _, _, _) => true)
                    .Authentication(new BasicAuthentication(elasticConfiguration.Login,
                            elasticConfiguration.Password))));

            builder.Services
                .AddKeyedScoped<IImportPreparationService, ElasticsearchImportPreparationService>("elastic");
            builder.Services.AddScoped<IRecordsReader<PoemWithAuthor>, PostgreRecordsReader>();
            builder.Services.AddScoped<IDataImporter<PoemWithAuthor>, ElasticsearchPoemsImporter>();
            builder.Services.AddKeyedScoped<IDataLoadingService, ElasticsearchDataLoadingService>("elastic");
        }
        else
        {
            builder.Services.AddKeyedScoped<IImportPreparationService, ElasticsearchImportPreparationService>("elastic",
                (_, _) => null!);
            builder.Services.AddScoped<IRecordsReader<PoemWithAuthor>, PostgreRecordsReader>(_ => null!);
            builder.Services.AddScoped<IDataImporter<PoemWithAuthor>, ElasticsearchPoemsImporter>(_ => null!);
            builder.Services.AddKeyedScoped<IDataLoadingService, ElasticsearchDataLoadingService>("elastic",
                (_, _) => null!);
        }

        #endregion

        builder.Services.AddHostedService<PoemsLoader>();

        var host = builder.Build();
        await host.RunAsync();
    }
}
