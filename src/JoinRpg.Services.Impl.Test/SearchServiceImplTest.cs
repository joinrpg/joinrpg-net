using JoinRpg.DataModel;
using JoinRpg.Services.Impl.Search;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Test;

public class SearchServiceImplTest
{
    private sealed class FakeSearchProvider(LinkType linkType = LinkType.ResultUser, params SearchResult[] results) : ISearchProvider
    {
        public LinkType LinkType => linkType;

        public Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString)
            => Task.FromResult<IReadOnlyCollection<SearchResult>>(results);
    }

    private sealed class FakeProjectScopedSearchProvider(LinkType linkType, params SearchResult[] results) : IProjectScopedSearchProvider
    {
        public LinkType LinkType => linkType;

        public Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString, ProjectIdentification? projectId)
            => Task.FromResult<IReadOnlyCollection<SearchResult>>(results);
    }

    private static SearchResult UserResult(int userId, string description, bool isPerfectMatch = false) => new()
    {
        LinkType = LinkType.ResultUser,
        Name = "Test User",
        Description = new MarkdownDbValue(description),
        Identification = userId.ToString(),
        ProjectId = null,
        IsPublic = true,
        IsActive = true,
        IsPerfectMatch = isPerfectMatch,
    };

    private static SearchResult CharacterResult(int characterId, string description) => new()
    {
        LinkType = LinkType.ResultCharacter,
        Name = "Test Character",
        Description = new MarkdownDbValue(description),
        Identification = characterId.ToString(),
        ProjectId = null,
        IsPublic = true,
        IsActive = true,
    };

    [Fact]
    public async Task SameUserFoundByTwoProviders_IsReturnedOnce()
    {
        // Как при поиске по "id123456": UserSearchByIdProvider и UserSearchBySocialProvider
        // находят одного и того же пользователя и раньше отдавали два разных SearchResult
        // (с разным Description), которые Distinct() не считал дубликатами.
        var service = new SearchServiceImpl(
        [
            new FakeSearchProvider(results: [UserResult(42, "ID: 42")]),
            new FakeSearchProvider(results: [UserResult(42, "")]),
        ]);

        var results = await service.SearchAsync(currentUserId: null, "id42");

        results.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SameUserFoundByTwoProviders_DescriptionsAreMerged()
    {
        var service = new SearchServiceImpl(
        [
            new FakeSearchProvider(results: [UserResult(42, "ID: 42")]),
            new FakeSearchProvider(results: [UserResult(42, "Найден в контактах")]),
        ]);

        var results = await service.SearchAsync(currentUserId: null, "id42");

        var result = results.ShouldHaveSingleItem();
        result.Description.Contents.ShouldBe("ID: 42, Найден в контактах");
    }

    [Fact]
    public async Task SameUserFoundByTwoProviders_WithSameDescription_IsNotDuplicated()
    {
        var service = new SearchServiceImpl(
        [
            new FakeSearchProvider(results: [UserResult(42, "ID: 42")]),
            new FakeSearchProvider(results: [UserResult(42, "ID: 42")]),
        ]);

        var results = await service.SearchAsync(currentUserId: null, "id42");

        var result = results.ShouldHaveSingleItem();
        result.Description.Contents.ShouldBe("ID: 42");
    }

    [Fact]
    public async Task DifferentUsers_AreBothReturned()
    {
        var service = new SearchServiceImpl(
        [
            new FakeSearchProvider(results: [UserResult(1, "ID: 1")]),
            new FakeSearchProvider(results: [UserResult(2, "ID: 2")]),
        ]);

        var results = await service.SearchAsync(currentUserId: null, "test");

        results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task LinkTypesFilter_OnlyMatchingProvidersAreCalled()
    {
        var service = new SearchServiceImpl(
        [
            new FakeSearchProvider(linkType: LinkType.ResultUser, results: [UserResult(1, "ID: 1")]),
            new FakeSearchProvider(linkType: LinkType.ResultCharacter, results: [CharacterResult(2, "ID: 2")]),
        ]);

        var results = await service.SearchAsync(currentUserId: null, "test", linkTypes: [LinkType.ResultCharacter]);

        var result = results.ShouldHaveSingleItem();
        result.LinkType.ShouldBe(LinkType.ResultCharacter);
    }

    [Fact]
    public async Task ProjectIdFilter_NonProjectScopedProvidersAreNotCalled()
    {
        var service = new SearchServiceImpl(
        [
            new FakeSearchProvider(linkType: LinkType.ResultUser, results: [UserResult(1, "ID: 1")]),
            new FakeProjectScopedSearchProvider(linkType: LinkType.ResultCharacter, results: [CharacterResult(2, "ID: 2")]),
        ]);

        var results = await service.SearchAsync(currentUserId: null, "test", projectId: new ProjectIdentification(1));

        var result = results.ShouldHaveSingleItem();
        result.LinkType.ShouldBe(LinkType.ResultCharacter);
    }
}
