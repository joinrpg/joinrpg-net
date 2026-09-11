using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

/// <summary>
/// Провайдер, который умеет сузить поиск до одного проекта. Реализует только этот
/// (3-арговый) метод — базовый <see cref="ISearchProvider.SearchAsync(int?, string)"/>
/// достаётся бесплатно через default-реализацию ниже, форвардящую с <c>projectId: null</c>.
/// </summary>
internal interface IProjectScopedSearchProvider : ISearchProvider
{
    Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString, ProjectIdentification? projectId);

    Task<IReadOnlyCollection<SearchResult>> ISearchProvider.SearchAsync(int? currentUserId, string searchString)
        => SearchAsync(currentUserId, searchString, projectId: null);
}
