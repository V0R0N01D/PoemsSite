using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Poems.Loader.Services.Interfaces;

namespace Poems.Loader.HostedServices;

/// <summary>
/// Applies database migrations and imports poems in a dedicated service scope.
/// </summary>
public class PoemsLoader(
    ILogger<PoemsLoader> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHostApplicationLifetime applicationLifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var postgres = scope.ServiceProvider.GetRequiredKeyedService<IDataLoadingService>("postgres");
            await postgres.LoadDataAsync(stoppingToken);

            if (configuration.GetValue<bool>("ElasticsearchEnabled"))
            {
                var elastic = scope.ServiceProvider.GetRequiredKeyedService<IDataLoadingService>("elastic");
                await elastic.LoadDataAsync(stoppingToken);
            }

            if (configuration.GetValue<bool>("AutoShutdown"))
                applicationLifetime.StopApplication();
            else
                logger.LogInformation("Для выключения нажмите Ctrl+C.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Environment.ExitCode = 1;
        }
        catch (Exception exception)
        {
            // Compose must not start the site after a failed migration or import.
            logger.LogError(exception, "Не удалось подготовить базу данных.");
            Environment.ExitCode = 1;
            applicationLifetime.StopApplication();
        }
    }
}
