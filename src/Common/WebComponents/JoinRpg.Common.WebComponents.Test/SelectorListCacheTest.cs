using Microsoft.AspNetCore.Components;

namespace JoinRpg.Common.WebComponents.Test;

public class SelectorListCacheTest
{
    private readonly TestNavigationManager navigationManager = new();

    private SelectorListCache CreateCache() => new(navigationManager);

    [Fact]
    public async Task ConcurrentCallsWithSameKeyLoadOnce()
    {
        var cache = CreateCache();
        var loads = 0;
        var source = new TaskCompletionSource<IEnumerable<int>>();

        var first = cache.GetOrLoad("key", () => { loads++; return source.Task; });
        var second = cache.GetOrLoad("key", () => { loads++; return source.Task; });
        source.SetResult([1, 2, 3]);

        (await first).ShouldBe([1, 2, 3]);
        (await second).ShouldBe([1, 2, 3]);
        loads.ShouldBe(1);
    }

    [Fact]
    public async Task DifferentKeysLoadSeparately()
    {
        var cache = CreateCache();
        var loads = 0;

        var first = await cache.GetOrLoad<int>("first", async () => { loads++; await Task.Yield(); return [1]; });
        var second = await cache.GetOrLoad<int>("second", async () => { loads++; await Task.Yield(); return [2]; });

        first.ShouldBe([1]);
        second.ShouldBe([2]);
        loads.ShouldBe(2);
    }

    [Fact]
    public async Task FailedLoadIsRetried()
    {
        var cache = CreateCache();
        var loads = 0;

        async Task<IEnumerable<int>> Load()
        {
            loads++;
            await Task.Yield();
            if (loads == 1)
            {
                throw new InvalidOperationException("Сеть упала");
            }
            return [42];
        }

        _ = await Should.ThrowAsync<InvalidOperationException>(() => cache.GetOrLoad("key", Load));
        (await cache.GetOrLoad("key", Load)).ShouldBe([42]);
        loads.ShouldBe(2);
    }

    /// <summary>
    /// Упавшую загрузку ждали несколько селекторов — исключение получает каждый, а не только первый,
    /// и в кэше она не остаётся.
    /// </summary>
    [Fact]
    public async Task FailedLoadFailsAllWaitersAndIsRetried()
    {
        var cache = CreateCache();
        var loads = 0;
        var source = new TaskCompletionSource<IEnumerable<int>>();

        var first = cache.GetOrLoad("key", () => { loads++; return source.Task; });
        var second = cache.GetOrLoad("key", () => { loads++; return source.Task; });
        source.SetException(new InvalidOperationException("Сеть упала"));

        _ = await Should.ThrowAsync<InvalidOperationException>(() => first);
        _ = await Should.ThrowAsync<InvalidOperationException>(() => second);
        loads.ShouldBe(1);

        (await cache.GetOrLoad<int>("key", () => { loads++; return Task.FromResult<IEnumerable<int>>([42]); })).ShouldBe([42]);
        loads.ShouldBe(2);
    }

    /// <summary>
    /// Пока шла загрузка A, кэш сбросили и тот же ключ загрузили заново (B). Когда A наконец упала,
    /// она не должна выкинуть из кэша чужую запись B.
    /// </summary>
    [Fact]
    public async Task FailedLoadDoesNotRemoveNewerEntry()
    {
        var cache = CreateCache();
        var loads = 0;
        var sourceA = new TaskCompletionSource<IEnumerable<int>>();

        var loadA = cache.GetOrLoad("key", () => { loads++; return sourceA.Task; });
        cache.Clear();
        (await cache.GetOrLoad<int>("key", () => { loads++; return Task.FromResult<IEnumerable<int>>([2]); })).ShouldBe([2]);

        sourceA.SetException(new InvalidOperationException("Сеть упала"));
        _ = await Should.ThrowAsync<InvalidOperationException>(() => loadA);

        (await cache.GetOrLoad<int>("key", () => { loads++; return Task.FromResult<IEnumerable<int>>([3]); })).ShouldBe([2]);
        loads.ShouldBe(2);
    }

    [Fact]
    public async Task ClearForgetsLoadedLists()
    {
        var cache = CreateCache();
        var loads = 0;
        Task<IEnumerable<int>> Load() => Task.FromResult<IEnumerable<int>>([++loads]);

        (await cache.GetOrLoad("key", Load)).ShouldBe([1]);
        cache.Clear();

        (await cache.GetOrLoad("key", Load)).ShouldBe([2]);
    }

    /// <summary>
    /// Переход без перезагрузки WebAssembly (например, NavigateTo без forceLoad) уводит на другую страницу —
    /// её селекторы не должны получить списки предыдущей.
    /// </summary>
    [Fact]
    public async Task NavigationForgetsLoadedLists()
    {
        var cache = CreateCache();
        var loads = 0;
        Task<IEnumerable<int>> Load() => Task.FromResult<IEnumerable<int>>([++loads]);

        (await cache.GetOrLoad("key", Load)).ShouldBe([1]);
        navigationManager.NavigateTo("/other-page");

        (await cache.GetOrLoad("key", Load)).ShouldBe([2]);
    }

    /// <summary>
    /// Список общий для всех селекторов с этим ключом: ни потребитель, ни клиент, отдавший изменяемую коллекцию,
    /// не должны поменять его остальным.
    /// </summary>
    [Fact]
    public async Task ResultIsReadOnlyCopy()
    {
        var cache = CreateCache();
        var loaded = new List<int> { 1, 2 };

        var result = await cache.GetOrLoad<int>("key", () => Task.FromResult<IEnumerable<int>>(loaded));
        loaded.Add(3);

        result.ShouldBe([1, 2]);
        result.ShouldBeAssignableTo<ICollection<int>>()!.IsReadOnly.ShouldBeTrue();
    }

    /// <summary>Переходы без браузера: каждый NavigateTo сразу сообщает о смене адреса.</summary>
    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Uri = ToAbsoluteUri(uri).ToString();
            NotifyLocationChanged(isInterceptedLink: false);
        }
    }
}
