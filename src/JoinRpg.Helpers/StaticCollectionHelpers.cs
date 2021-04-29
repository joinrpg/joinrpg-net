using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    public static class StaticCollectionHelpers
    {
        private static readonly Random Rng = new();

        public static Dictionary<string, string> ToDictionary(this NameValueCollection collection) => collection.AllKeys.ToDictionary(key => key, key => collection[key]);

        [NotNull, ItemNotNull]
        public static ISet<T> FlatTree<T>(this T obj,
            Func<T, IEnumerable<T>> parentSelectorFunc,
            bool includeSelf = true)
            where T : IEquatable<T>
        {
            var groups = new HashSet<T>();
            if (obj == null)
            {
                return groups;
            }

            if (includeSelf)
            {
                _ = groups.Add(obj);
            }

            FlatTreeImpl(groups, obj, parentSelectorFunc);
            return groups;
        }

        private static void FlatTreeImpl<T>(ISet<T> flattedcharacterGroupIds,
            T group1,
            Func<T, IEnumerable<T>> parentSelectorFunc)
        {
            foreach (var characterGroup in parentSelectorFunc(group1)
                .Except(flattedcharacterGroupIds))
            {
                FlatTreeImpl(flattedcharacterGroupIds, characterGroup, parentSelectorFunc);
                _ = flattedcharacterGroupIds.Add(characterGroup);
            }
        }

        public static IEnumerable<T> UnionIf<T>(this IEnumerable<T> source,
            IEnumerable<T> enumerable,
            bool add)
            => source.Union(add ? enumerable : Enumerable.Empty<T>());

        public static IEnumerable<T> Union<T>(this IEnumerable<T> source, T t) => source.Union(new[] { t });

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        {
            foreach (var i in source)
            {
                if (i is T v)
                {
                    yield return v;
                }
            }
        }

        public static IEnumerable<int> WhereNotNull(this IEnumerable<int?> source)
        {
            foreach (var i in source)
            {
                if (i is int v)
                {
                    yield return v;
                }
            }
        }

        public static IEnumerable<T> OrEmptyList<T>(this IEnumerable<T> collection) => collection ?? Enumerable.Empty<T>();

        public static IEnumerable<T> Shuffle<T>([NotNull]
            this IEnumerable<T> source) => Shuffle(source, Rng);

        public static IEnumerable<T> Shuffle<T>([NotNull]
            this IEnumerable<T> source,
            Random random)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var sourceArray = source.ToArray();

            for (var n = 0; n < sourceArray.Length; n++)
            {
                var k = random.Next(n, sourceArray.Length);
                yield return sourceArray[k];

                sourceArray[k] = sourceArray[n];
            }
        }

        public static IEnumerable<T> UnionUntilTotalCount<T>([NotNull]
            this IReadOnlyCollection<T> alreadyTaken,
            [NotNull]
            IEnumerable<T> toAdd,
            int totalLimit)
        {
            if (alreadyTaken == null)
            {
                throw new ArgumentNullException(nameof(alreadyTaken));
            }

            if (toAdd == null)
            {
                throw new ArgumentNullException(nameof(toAdd));
            }

            return alreadyTaken.Union(
                toAdd
                    .Except(alreadyTaken)
                    .Take(Math.Max(0, totalLimit - alreadyTaken.Count)));
        }

        public static IEnumerable<int> GetRandomSource(this Random random)
        {
            while (true)
            {
                yield return random.Next();
            }
        }
    }
}
