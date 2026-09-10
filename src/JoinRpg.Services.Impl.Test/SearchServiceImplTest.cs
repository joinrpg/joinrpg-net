using JoinRpg.DataModel;
using JoinRpg.Services.Impl.Search;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Test;

public class SearchServiceImplTest
{
    private sealed class FakeSearchProvider(params SearchResult[] results) : ISearchProvider
    {
        public Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString)
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

    [Fact]
    public async Task SameUserFoundByTwoProviders_IsReturnedOnce()
    {
        // Как при поиске по "id123456": UserSearchByIdProvider и UserSearchBySocialProvider
        // находят одного и того же пользователя и раньше отдавали два разных SearchResult
        // (с разным Description), которые Distinct() не считал дубликатами.
        var service = new SearchServiceImpl(
        [
            new FakeSearchProvider(UserResult(42, "ID: 42")),
            new FakeSearchProvider(UserResult(42, "")),
        ]);

        var results = await service.SearchAsync(currentUserId: null, "id42");

        results.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SameUserFoundByTwoProviders_DescriptionsAreMerged()
    {
        var service = new SearchServiceImpl(
        [
            new FakeSearchProvider(UserResult(42, "ID: 42")),
            new FakeSearchProvider(UserResult(42, "Найден в контактах")),
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
            new FakeSearchProvider(UserResult(42, "ID: 42")),
            new FakeSearchProvider(UserResult(42, "ID: 42")),
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
            new FakeSearchProvider(UserResult(1, "ID: 1")),
            new FakeSearchProvider(UserResult(2, "ID: 2")),
        ]);

        var results = await service.SearchAsync(currentUserId: null, "test");

        results.Count.ShouldBe(2);
    }
}
