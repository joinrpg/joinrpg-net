using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Private copy of the reflection helpers from JoinRpg.Helpers, kept here so this package
/// does not depend on JoinRpg.Helpers.
/// </summary>
internal static class ReflectionExtensions
{
    public static PropertyInfo? AsPropertyAccess<T>(this Expression<Func<T>> expression)
        => AsPropertyAccess(expression.Body);

    private static PropertyInfo? AsPropertyAccess(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert } convertExpression
            ? AsPropertyAccess(convertExpression.Operand)
            : (expression as MemberExpression)?.Member as PropertyInfo;

    public static string GetDisplayName(this MemberInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        return propertyInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? propertyInfo.GetCustomAttribute<DisplayAttribute>()?.GetName()
            ?? propertyInfo.Name;
    }

    public static string GetDisplayName<TEnum>(this TEnum enumValue) where TEnum : notnull, Enum
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        return enumValue.GetEnumAttribute<DisplayNameAttribute>()?.DisplayName
            ?? enumValue.GetEnumAttribute<DisplayAttribute>()?.GetName()
            ?? enumValue.ToString();
    }

    public static string? GetDescription<TEnum>(this TEnum enumValue) where TEnum : notnull, Enum
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        return enumValue.GetEnumAttribute<DescriptionAttribute>()?.Description
            ?? enumValue.GetEnumAttribute<DisplayAttribute>()?.GetDescription();
    }

    private static TAttribute? GetEnumAttribute<TAttribute>(this Enum enumValue)
        where TAttribute : Attribute =>
        enumValue.GetType()
            .GetField(enumValue.ToString())?
            .GetCustomAttribute<TAttribute>();
}
