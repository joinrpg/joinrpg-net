using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Returns values of an enumeration of type <typeparamref name="T"/>
        /// </summary>
        [PublicAPI]
        public static IEnumerable<T> GetValues<T>() => Enum.GetValues(typeof(T)).Cast<T>();
    }
}
