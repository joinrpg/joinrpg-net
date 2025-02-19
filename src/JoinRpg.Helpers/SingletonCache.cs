using System.Collections.Concurrent;

namespace JoinRpg.Helpers;

public class SingletonCache<TKey, TValue>
    where TKey : notnull, IEquatable<TKey>
    where TValue : class
{
    private readonly ConcurrentDictionary<TKey, TValue> cache = new();
    public TValue? TryGet(TKey key) => cache.TryGetValue(key, out var value) ? value : null;

    public void Set(TKey key, TValue value) => _ = cache.AddOrUpdate(key, _ => value, (_, _) => value);
}
