namespace Poems.Loader.Services.Interfaces;

public interface IRecordsReader<T>
{
    Task<IList<T>> ReadRecordsAsync(int maxCount, int startIndex = 0,
        CancellationToken cancellationToken = default);
}