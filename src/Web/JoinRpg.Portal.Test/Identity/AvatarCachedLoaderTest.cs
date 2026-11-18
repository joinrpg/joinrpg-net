using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebInfrastructure.Cache;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Portal.Test.Identity;

/// <summary>
/// Пакетная загрузка аватарок через общий с одиночной кеш (ADR026, #5774).
/// </summary>
public class AvatarCachedLoaderTest
{
    private const int Size = 64;

    private static readonly AvatarIdentification First = new(1);
    private static readonly AvatarIdentification Second = new(2);
    private static readonly AvatarIdentification Third = new(3);
    private static readonly AvatarIdentification Missing = new(404);

    private static readonly UserAvatar[] Avatars = [Avatar(First), Avatar(Second), Avatar(Third)];

    private readonly SingletonCache<AvatarCacheKey, Task<AvatarInfo>> cache = new();

    [Fact]
    public async Task GetAvatars_CacheHit_IsNotRequestedFromDatabase()
    {
        var warmUp = new FakeUserRepository(avatars: Avatars);
        _ = await CreateLoader(warmUp).GetAvatar(First, Size);

        var repository = new FakeUserRepository(avatars: Avatars);
        var result = await CreateLoader(repository).GetAvatars([First, Second, Third], Size);

        repository.AvatarRequests.ShouldHaveSingleItem().ShouldBe([Second, Third], ignoreOrder: true);
        result.Keys.ShouldBe([First, Second, Third], ignoreOrder: true);
    }

    [Fact]
    public async Task GetAvatars_AllCached_DoesNotGoToDatabase()
    {
        _ = await CreateLoader(new FakeUserRepository(avatars: Avatars)).GetAvatars([First, Second], Size);

        var result = await CreateLoader(FakeUserRepository.MustNotBeCalled("всё уже в кеше"))
            .GetAvatars([First, Second], Size);

        result.Keys.ShouldBe([First, Second], ignoreOrder: true);
    }

    [Fact]
    public async Task GetAvatars_Misses_AreLoadedInSingleRequest()
    {
        var repository = new FakeUserRepository(avatars: Avatars);

        var result = await CreateLoader(repository).GetAvatars([First, Second, Third], Size);

        repository.CallCount.ShouldBe(1);
        result[Second].ShouldBe(new AvatarInfo(new Uri("https://example.com/2.png"), Size));
    }

    [Fact]
    public async Task GetAvatars_Duplicates_AreCollapsed()
    {
        var repository = new FakeUserRepository(avatars: Avatars);

        var result = await CreateLoader(repository).GetAvatars([First, First, Second], Size);

        repository.AvatarRequests.ShouldHaveSingleItem().ShouldBe([First, Second], ignoreOrder: true);
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAvatars_Empty_DoesNotGoToDatabase()
    {
        var result = await CreateLoader(FakeUserRepository.MustNotBeCalled("запрашивать нечего"))
            .GetAvatars([], Size);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAvatars_MissingInDatabase_IsSkippedAndRetriedNextTime()
    {
        var repository = new FakeUserRepository(avatars: Avatars);
        var loader = CreateLoader(repository);

        var result = await loader.GetAvatars([First, Missing], Size);
        result.Keys.ShouldBe([First]);

        _ = await loader.GetAvatars([First, Missing], Size);
        // Отсутствие не запоминается навсегда: пропавшую аватарку спрашиваем снова, найденную — нет.
        repository.AvatarRequests.Count.ShouldBe(2);
        repository.AvatarRequests[1].ShouldBe([Missing]);
    }

    [Fact]
    public async Task GetAvatar_MissingInDatabase_ThrowsNotFound()
    {
        var loader = CreateLoader(new FakeUserRepository(avatars: Avatars));

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(() => loader.GetAvatar(Missing, Size));
    }

    [Fact]
    public async Task GetAvatars_DifferentSize_IsSeparateCacheEntry()
    {
        _ = await CreateLoader(new FakeUserRepository(avatars: Avatars)).GetAvatars([First], Size);

        var repository = new FakeUserRepository(avatars: Avatars);
        var result = await CreateLoader(repository).GetAvatars([First], 128);

        repository.CallCount.ShouldBe(1);
        result[First].Size.ShouldBe(128);
    }

    [Fact]
    public async Task GetAvatars_ConcurrentRequests_LoadOnce()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new FakeUserRepository(avatars: Avatars, beforeAvatarsLoad: () => gate.Task);
        var loader = CreateLoader(repository);

        var first = loader.GetAvatars([First, Second], Size);
        var second = loader.GetAvatars([Second, First], Size);
        gate.SetResult();

        (await first).Count.ShouldBe(2);
        (await second).Count.ShouldBe(2);
        // Второй запрос застал загрузку первого и дождался её, а не пошёл в БД сам.
        repository.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetAvatars_DatabaseFailure_IsRetriedNextTime()
    {
        var failing = new FakeUserRepository(
            avatars: Avatars,
            beforeAvatarsLoad: () => Task.FromException(new InvalidOperationException("БД недоступна")));
        _ = await Should.ThrowAsync<InvalidOperationException>(() => CreateLoader(failing).GetAvatars([First], Size));

        var repository = new FakeUserRepository(avatars: Avatars);
        var result = await CreateLoader(repository).GetAvatars([First], Size);

        repository.CallCount.ShouldBe(1);
        result.Keys.ShouldBe([First]);
    }

    [Fact]
    public async Task GetAvatars_ResolverFailure_DoesNotHangAndIsRetried()
    {
        var broken = new AvatarCachedLoader(new FakeUserRepository(avatars: Avatars), cache, new ThrowingAvatarUriResolver());
        _ = await Should.ThrowAsync<UriFormatException>(() => broken.GetAvatars([First, Second], Size));

        var repository = new FakeUserRepository(avatars: Avatars);
        var result = await CreateLoader(repository).GetAvatars([First, Second], Size)
            .WaitAsync(TimeSpan.FromSeconds(10));

        repository.CallCount.ShouldBe(1);
        result.Count.ShouldBe(2);
    }

    private AvatarCachedLoader CreateLoader(FakeUserRepository repository)
        => new(repository, cache, new FakeAvatarUriResolver());

    private sealed class ThrowingAvatarUriResolver : IAvatarUriResolver
    {
        public Uri GetAvatarUri(string? cachedUri, string originalUri) => throw new UriFormatException("Кривой адрес");
    }

    private static UserAvatar Avatar(AvatarIdentification id) => new()
    {
        UserAvatarId = id.Value,
        UserId = id.Value,
        OriginalUri = $"https://example.com/{id.Value}.png",
        IsActive = true,
    };
}
