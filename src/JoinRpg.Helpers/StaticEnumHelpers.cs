using System;
using System.Reflection;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    /// <summary>
    /// Helpers for <see cref="Enum"/> type
    /// </summary>
    public static class StaticEnumHelpers
    {
        /// <summary>
        /// Returns custom attribute defined by <typeparamref name="TAttribute"/> attached to an enumeration value
        /// </summary>
        [PublicAPI, CanBeNull]
        public static TAttribute? GetAttribute<TAttribute>(this Enum enumValue)
            where TAttribute : Attribute =>
            enumValue.GetType()
                .GetField(enumValue.ToString())?
                .GetCustomAttribute<TAttribute>();
    }
}
