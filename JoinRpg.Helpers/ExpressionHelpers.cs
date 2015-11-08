using System;
using System.Linq.Expressions;

namespace JoinRpg.Helpers
{
  public static class ExpressionHelpers
  {
    public static string AsPropertyName<T1, T2>(this Expression<Func<T1, T2>>  expression)
    {
      return ((MemberExpression)expression.Body).Member.Name;
    }
  }
}
