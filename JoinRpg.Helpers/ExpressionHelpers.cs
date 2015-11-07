using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

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
