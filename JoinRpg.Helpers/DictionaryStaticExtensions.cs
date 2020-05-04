using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    public static class DictionaryStaticExtensions
    {
        public static TValue? GetValueOrDefault<TKey, TValue>([NotNull]
            this IReadOnlyDictionary<TKey, TValue> data,
            TKey key)
            where TValue: class
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return data.ContainsKey(key) ? data[key] : default;
        }
    }
}
