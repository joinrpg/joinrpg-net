using System.Reflection;

namespace PscbApi;

/// <summary>
/// Extensions for enums
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Returns first attribute of specified type <typeparamref name="TAttribute"/>
    /// </summary>
    public static TAttribute? GetCustomAttribute<TAttribute>(this Enum self)
        where TAttribute : Attribute
        => self.GetType()
            .GetField(self.ToString())?
            .GetCustomAttribute<TAttribute>(inherit: true);

    /// <summary>
    /// Returns identifier associated using <see cref="IdentifierAttribute"/> to enumeration value
    /// </summary>
    public static string GetIdentifier(this Enum self)
        => self.GetCustomAttribute<IdentifierAttribute>()?.Identifier ?? Enum.GetName(self.GetType(), self) ?? throw new InvalidOperationException("Enum value {self} unexpected");
}
