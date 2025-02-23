namespace Poems.Loader.Services.Interfaces;

public interface IDataLoadingService
{
    Task LoadDataAsync(CancellationToken stoppingToken);
}