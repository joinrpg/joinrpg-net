using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    public static class StaticEnumHelpers
    {
        [PublicAPI, CanBeNull]
        public static TAttribute? GetAttribute<TAttribute>(this Enum enumValue)
            where TAttribute : Attribute
        {
            return enumValue.GetType()
                .GetField(enumValue.ToString())?
                .GetCustomAttribute<TAttribute>();
        }
    }
}
