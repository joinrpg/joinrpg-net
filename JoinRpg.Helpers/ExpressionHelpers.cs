using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    public static class ExpressionHelpers
    {
        [CanBeNull]
        public static string AsPropertyName<T1, T2>(this Expression<Func<T1, T2>> expression)
        {
            return expression.AsPropertyAccess()?.Name;
        }

        [CanBeNull]
        public static PropertyInfo AsPropertyAccess<T1, T2>(
            this Expression<Func<T1, T2>> expression)
        {
            if (expression.Body is UnaryExpression convertExpression &&
                convertExpression.NodeType == ExpressionType.Convert)
            {
                return AsPropertyAccess(convertExpression.Operand);
            }

            return AsPropertyAccess(expression.Body);
        }

        [CanBeNull]
        private static PropertyInfo AsPropertyAccess(Expression body)
        {
            var memberExpression = body as MemberExpression;
            return memberExpression?.Member as PropertyInfo;
        }

        public static string AsPropertyName<T1>(this Expression<Func<T1>> expression)
        {
            return ((MemberExpression) expression.Body).Member.Name;
        }
    }
}
