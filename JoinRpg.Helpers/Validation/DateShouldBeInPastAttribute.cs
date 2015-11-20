using System;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Helpers.Validation
{

  public class DateShouldBeInPastAttribute : ValidationAttribute
  {
    //TODO: Implement client validation
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
      DateTime dt = (DateTime)value;
      if (dt.Date <= DateTime.UtcNow.Date)
      {
        return ValidationResult.Success;
      }

      return new ValidationResult(ErrorMessage ?? "Make sure your date is >= than today");
    }

  }
}
