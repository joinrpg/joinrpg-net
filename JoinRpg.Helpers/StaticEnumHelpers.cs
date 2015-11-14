using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JoinRpg.Helpers
{
  public static class StaticEnumHelpers
  {
    public static TAttribute GetAttribute<TAttribute>(this Enum enumValue) where TAttribute:Attribute
    {
      return enumValue.GetType()
        .GetMember(enumValue.ToString())
        .First()
        .GetCustomAttribute<TAttribute>();
    }

    public static IEnumerable<T> GetValues<T>()
    {
      return Enum.GetValues(typeof (T)).Cast<T>();
    }

    public static ICollection<T> OrEmptyList<T>(this ICollection<T> collection)
    {
      return collection ?? new T[] {};
    }
  }

}
