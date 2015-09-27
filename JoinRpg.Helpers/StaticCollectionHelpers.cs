using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace JoinRpg.Helpers
{
  public static class StaticCollectionHelpers
  {
    public static Dictionary<string, string> ToDictionary(this NameValueCollection collection)
    {
      return collection.AllKeys.ToDictionary(key => key, key => collection[key]);
    }

    public static IEnumerable<T> FlatTree<T>(this T obj, Func<T, IEnumerable<T>> parentSelectorFunc, bool includeSelf = true)
    {
      var groups = new List<T>();
      if (includeSelf)
      {
        groups.Add(obj);
      }
      FlatTreeImpl(groups, obj, parentSelectorFunc);
      return groups;
    }

    private static void FlatTreeImpl<T>(ICollection<T> flattedcharacterGroupIds, T group1, Func<T, IEnumerable<T>> parentSelectorFunc)
    {
      
      foreach (var characterGroup in parentSelectorFunc(group1))
      {
        FlatTreeImpl(flattedcharacterGroupIds, characterGroup, parentSelectorFunc);
        flattedcharacterGroupIds.Add(characterGroup);
      }
    }

    public static IEnumerable<T> UnionIf<T>(this IEnumerable<T> source, T @object, bool add)
    {
      var self = add ? new[] {@object} : new T[] {};
      var claimSources = source.Union(self);
      return claimSources;
    }

    public static IEnumerable<T> Union<T>(this IEnumerable<T> source, T t)
    {
      return source.Union(new [] {t});
    }
  }
}
