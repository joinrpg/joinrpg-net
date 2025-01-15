using System.ComponentModel.DataAnnotations;

namespace JoinRpg.WebComponents;

/// <summary>
/// Makes boolean fields required (i.e. checkbox must be checked)
/// </summary>
/// <remarks>
/// Includes [Required] attribute logic, therefore no sense to use both attributes at once
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class BooleanRequiredAttribute : RequiredAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (value.GetType() != typeof(bool))
        {
            throw new InvalidOperationException("Can only be used on boolean properties.");
        }

        return (bool)value;
    }
}
