using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
  public static class DisplayAttributeHelper
  {
    [NotNull]
    public static string GetDisplayName(this Enum enumValue) =>
      enumValue.GetAttribute<DisplayAttribute>()?.GetName() ?? enumValue.ToString();

    [NotNull]
    public static string GetDisplayName([NotNull] this PropertyInfo propertyInfo)
    {
      if (propertyInfo == null) throw new ArgumentNullException(nameof(propertyInfo));

      return propertyInfo.GetCustomAttribute<DisplayAttribute>()?.Name ?? propertyInfo.Name;
    }
  }
}
