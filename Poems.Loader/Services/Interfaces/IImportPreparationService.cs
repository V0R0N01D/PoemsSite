namespace Poems.Loader.Services.Interfaces;

public interface IImportPreparationService
{
    Task<bool> PrepareDestinationAsync(CancellationToken cancellationToken = default);
}