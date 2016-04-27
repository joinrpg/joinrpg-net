using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.Services.Export.Internal
{
  internal static class LambdaHelpers
  {
    public static Func<TBaseObject, object> CompileGetter<TBaseObject>(PropertyInfo propertyInfo)
    {
      var parameterExpression = Expression.Parameter(typeof(TBaseObject));
      return Expression.Lambda<Func<TBaseObject, object>>(
        Expression.Convert(
          Expression.Property(
            parameterExpression, propertyInfo), typeof(object)), parameterExpression).Compile();
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

    public static Func<object, string> GetEnumerableConvertor(Func<object, string> toStringConvertor)
    {
      return o => ((IEnumerable) o).Cast<object>().Select(toStringConvertor).Join(", ");
    }
  }
}
