using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    public static class LambdaHelpers
    {
        public static Func<object, object> CompileGetter(PropertyInfo propertyInfo, Type targetType)
        {
            var parameterExpression = Expression.Parameter(typeof(object));
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(parameterExpression, targetType),
                        propertyInfo),
                    typeof(object)),
                parameterExpression).Compile();
        }

        [CanBeNull]
        public static Type GetEnumerableType(Type type)
        {
            foreach (var intType in type.GetInterfaces())
            {
                if (intType.IsGenericType
                    && intType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return intType.GetGenericArguments()[0];
                }
            }

            return null;
        }

        public static Func<object, string> GetEnumerableConvertor(
            Func<object, string> toStringConvertor)
        {
            return o =>
                ((IEnumerable) o).Cast<object>().Select(toStringConvertor).JoinStrings(", ");
        }
    }
}
