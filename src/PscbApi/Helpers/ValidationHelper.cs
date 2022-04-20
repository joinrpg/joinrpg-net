// ReSharper disable CheckNamespace
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace PscbApi
{
    internal static class ValidationHelper
    {

        internal static bool TryGetMemberInfo<T>(this Expression body, out T memberInfo)
            where T : MemberInfo
        {
            memberInfo = body is MemberExpression me && me.Member is T mi ? mi : null;
            return memberInfo != null;
        }

        internal static bool TryGetWritablePropertyInfo(this Expression body, out PropertyInfo propInfo)
        {
            propInfo = body.TryGetMemberInfo(out PropertyInfo result) && result.CanWrite ? result : null;
            return propInfo != null;
        }

        internal static void CheckStringPropertyLength<T>(this T instance, Expression<Func<T, string>> selector)
            where T : class
        {
            if (selector.Body.TryGetWritablePropertyInfo(out PropertyInfo pi))
            {
                var maxLengthConstraint = pi.GetCustomAttribute<MaxLengthAttribute>()?.Length
                    ?? pi.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;
                if (maxLengthConstraint.HasValue)
                {
                    pi.SetValue(
                        instance,
                        ((string)pi.GetValue(instance)).TrimLength(maxLengthConstraint.Value));
                }
            }
        }


        internal static void ValidateProperty<T, V>(this T instance, Expression<Func<T, V>> selector)
            where T : class
        {
            if (selector.Body.TryGetWritablePropertyInfo(out PropertyInfo pi))
            {
                Validator.ValidateValue(
                    pi.GetValue(instance),
                    new ValidationContext(instance),
                    pi
                        .GetCustomAttributes(typeof(ValidationAttribute), true)
                        .Cast<ValidationAttribute>());
            }
        }
    }
}
