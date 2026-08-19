using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Helpers.Validation;

public class DateShouldBeInPastAttribute : ValidationAttribute
{
    //TODO: Implement client validation
    protected override ValidationResult? IsValid(object? value, ValidationContext? validationContext)
    {
        if (value is null)
        {
            return new ValidationResult(ErrorMessage ?? "Date is null");
        }
        var date = value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            DateOnly d => d,
            _ => throw new ArgumentException($"Unsupported type {value.GetType()}", nameof(value)),
        };
        if (date <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(+1)
        ) //TODO[UTC]: if everyone properly uses UTC, we don't have to do +1
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? "Make sure your date is >= than today");
    }
}
