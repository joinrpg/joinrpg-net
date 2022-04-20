using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Helpers.Validation
{
    public class DateShouldBeInPastAttribute : ValidationAttribute
    {
        //TODO: Implement client validation
        protected override ValidationResult IsValid(object value,
            ValidationContext validationContext)
        {
            var dt = (DateTime)value;
            if (dt.Date <= DateTime.UtcNow.Date.AddDays(+1)
            ) //TODO[UTC]: if everyone properly uses UTC, we don't have to do +1
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? "Make sure your date is >= than today");
        }
    }
}
