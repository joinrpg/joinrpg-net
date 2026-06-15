using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace JoinRpg.Helpers;

/// <summary>
/// Helpers for attributes with text resources
/// </summary>
public static class DisplayAttributeHelper
{
    /// <summary>
    /// Returns name of a member entry specified by <see cref="DisplayNameAttribute"/> or <see cref="DisplayAttribute"/>
    /// </summary>
    public static string GetDisplayName(this MemberInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        return propertyInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? propertyInfo.GetCustomAttribute<DisplayAttribute>()?.GetName()
            ?? propertyInfo.Name;
    }

    /// <summary>
    /// Returns name specified by <see cref="DisplayNameAttribute"/> or <see cref="DisplayAttribute"/>
    /// </summary>
    public static string GetDisplayName<TEnum>(this TEnum enumValue) where TEnum : notnull, Enum
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        return enumValue.GetAttribute<DisplayNameAttribute>()?.DisplayName
            ?? enumValue.GetAttribute<DisplayAttribute>()?.GetName()
            ?? enumValue.ToString();
    }

    /// <summary>
    /// Returns short name specified by <see cref="DisplayAttribute"/>
    /// </summary>
    public static string? GetShortName<TEnum>(this TEnum enumValue) where TEnum : notnull, Enum
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        return enumValue.GetAttribute<DisplayAttribute>()?.GetShortName();
    }

    /// <summary>
    /// Returns description specified by <see cref="DescriptionAttribute"/> or <see cref="DisplayAttribute"/>
    /// </summary>
    public static string? GetDescription<TEnum>(this TEnum enumValue) where TEnum : notnull, Enum
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        return enumValue.GetAttribute<DescriptionAttribute>()?.Description
            ?? enumValue.GetAttribute<DisplayAttribute>()?.GetDescription();
    }
}
