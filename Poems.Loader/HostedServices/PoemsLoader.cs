using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Poems.Common.Models.Elasticsearch;
using Poems.Loader.Services.Interfaces;
using Poems.Loader.Models;

namespace Poems.Loader.HostedServices;

/// <summary>
/// Background service for loading poems into the database.
/// </summary>
public class PoemsLoader : BackgroundService
{
    private readonly ILogger<PoemsLoader> _logger;
    private readonly IDataLoadingService _postgresDataLoadingService;
    private readonly IDataLoadingService _elasticsearchDataLoadingService;
    private readonly bool _elasticsearchEnabled;
    private readonly bool _autoShutdown;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoemsLoader"/> class.
    /// </summary>
    /// <param name="logger">Logger for logging information.</param>
    /// <param name="postgresDataLoadingService"></param>
    /// <param name="elasticsearchDataLoadingService"></param>
    /// <param name="configuration"></param>
    /// <param name="hostApplicationLifetime"></param>
    public PoemsLoader(ILogger<PoemsLoader> logger,
        [FromKeyedServices("postgres")] IDataLoadingService postgresDataLoadingService,
        [FromKeyedServices("elastic")] IDataLoadingService elasticsearchDataLoadingService,
        IConfiguration configuration,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _postgresDataLoadingService = postgresDataLoadingService;
        _elasticsearchDataLoadingService = elasticsearchDataLoadingService;
        _elasticsearchEnabled = configuration.GetValue<bool>("ElasticsearchEnabled");
        _autoShutdown = configuration.GetValue<bool>("AutoShutdown");
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _postgresDataLoadingService.LoadDataAsync(stoppingToken);

        if (_elasticsearchEnabled)
            await _elasticsearchDataLoadingService.LoadDataAsync(stoppingToken);

        if (_autoShutdown)
        {
            _hostApplicationLifetime.StopApplication();
            return;
        }

        _logger.LogInformation("Для выключения нажмите Ctrl+C.");
    }
}