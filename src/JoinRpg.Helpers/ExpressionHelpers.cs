using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
    public static class ExpressionHelpers
    {
        [CanBeNull]
        public static string? AsPropertyName<T1, T2>(this Expression<Func<T1, T2>> expression)
            => expression.AsPropertyAccess()?.Name;

        public static string? AsPropertyName<T1>(this Expression<Func<T1>> expression)
            => expression.AsPropertyAccess()?.Name;

        [CanBeNull]
        public static PropertyInfo? AsPropertyAccess<T1, T2>(this Expression<Func<T1, T2>> expression)
            => AsPropetyAccessImpl(expression.Body);

        [CanBeNull]
        public static PropertyInfo? AsPropertyAccess<T>(this Expression<Func<T>> expression)
         => AsPropetyAccessImpl(expression.Body);

        private static PropertyInfo? AsPropetyAccessImpl(Expression expression)
        {
            if (expression is UnaryExpression convertExpression &&
                convertExpression.NodeType == ExpressionType.Convert)
            {
                return AsPropertyAccess(convertExpression.Operand);
            }

            return AsPropertyAccess(expression);
        }

        [CanBeNull]
        private static PropertyInfo? AsPropertyAccess(Expression body)
        {
            return (body as MemberExpression)?.Member as PropertyInfo;
        }
    }
}
