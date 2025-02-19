using System.Collections.Concurrent;

namespace JoinRpg.Helpers;

public class PerRequestCache<TKey, TValue>
    where TKey : notnull, IEquatable<TKey>
    where TValue : class
{
    private readonly ConcurrentDictionary<TKey, TValue> cache = new();
    public TValue? TryGet(TKey key)
    {
        if (cache.TryGetValue(key, out var value))
        {
            return value;
        }
        else
        {
            return null;
        }
    }

    public void Set(TKey key, TValue value)
    {
        _ = cache.AddOrUpdate(key, _ => value, (_, _) => value);
    }


}
