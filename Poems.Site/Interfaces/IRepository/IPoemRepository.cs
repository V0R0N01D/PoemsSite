using Poems.Site.Models.Dtos;

namespace Poems.Site.Interfaces.IRepository;

public interface IPoemRepository
{
	Task<IEnumerable<PoemShortDto>> SearchPoemsAsync(string query, int maxCount,
		float minimalRank, CancellationToken cancellationToken);
}
