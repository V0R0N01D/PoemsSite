using Poems.Site.Interfaces.IRepository;
using Poems.Site.Interfaces.IServices;
using Poems.Site.Models.Dtos;

namespace Poems.Site.Services;

internal class PoemService : IPoemService
{
	private readonly IPoemRepository _poemRepository;

	public PoemService(IPoemRepository poemRepository)
	{
		_poemRepository = poemRepository;
	}

	public async Task<IEnumerable<PoemShortDto>> SearchPoemsAsync(string query, int maxCount = 30,
		float minimalRank = 0.3F, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(query))
			return Enumerable.Empty<PoemShortDto>();

		if (maxCount < 1)
			maxCount = 30;

		if (minimalRank < 0 || minimalRank > 1)
			minimalRank = 0.3F;

		return await _poemRepository.SearchPoemsAsync(query, maxCount, minimalRank, cancellationToken);
	}
}
