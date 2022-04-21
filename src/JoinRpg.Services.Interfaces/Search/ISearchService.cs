using JetBrains.Annotations;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, [NotNull] string searchString);
}
