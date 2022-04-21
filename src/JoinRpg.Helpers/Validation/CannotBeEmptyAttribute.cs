using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Helpers.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CannotBeEmptyAttribute : RequiredAttribute
{
    public override bool IsValid(object value)
    {
        var list = value as IEnumerable;
        return list != null && list.GetEnumerator().MoveNext();
    }
}
