using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace JoinRpg.Web.Models.UserProfile;

/// <summary>
/// Makes boolean fields required (i.e. checkbox must be checked)
/// </summary>
/// <remarks>
/// Includes [Required] attribute logic, therefore no sense to use both attributes at once
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
[Obsolete("Работает только с ASP.NET MVC, поэтому лучше не использовать.")]
//TODO: <FrameworkReference Include="Microsoft.AspNetCore.App" /> нужно только для IClientModelValidator
public class BooleanRequiredAttribute : RequiredAttribute, IClientModelValidator
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

    /// <inheritdoc cref="IClientModelValidator.AddValidation"/>
    public void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute("data-val", "true");
        MergeAttribute("data-val-enforcetrue", ErrorMessage);

        void MergeAttribute(string key, string? value)
        {
            if (context.Attributes.ContainsKey(key))
            {
                return;
            }

            if (value is null)
            {
                return;
            }

            context.Attributes.Add(key, value);
        }
    }
}
