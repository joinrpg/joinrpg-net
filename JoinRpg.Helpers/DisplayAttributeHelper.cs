using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    public static class DisplayAttributeHelper
    {
        [NotNull]
        public static string GetDisplayName(
            this Enum enumValue)
        {
            if (enumValue == null)
            {
                return "";
            }

            return enumValue.GetAttribute<DisplayAttribute>()?.GetName() ?? enumValue.ToString();
        }

        public static string? GetShortNameOrDefault([NotNull]
            this Enum enumValue)
        {
            if (enumValue == null)
            {
                throw new ArgumentNullException(nameof(enumValue));
            }

            return enumValue.GetAttribute<DisplayAttribute>()?.GetShortName();
        }

        [NotNull]
        public static string GetDisplayName([NotNull]
            this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            return propertyInfo.GetCustomAttribute<DisplayAttribute>()?.Name ?? propertyInfo.Name;
        }

        /// <summary>
        /// Returns description for enum value
        /// </summary>
        public static string? GetDescription(this Enum enumValue)
        {
            if (enumValue == null)
            {
                throw new ArgumentNullException(nameof(enumValue));
            }

            return enumValue.GetAttribute<DisplayAttribute>()
                    ?.Description
                ?? enumValue.GetAttribute<DescriptionAttribute>()
                    ?.Description;
        }
    }
}
