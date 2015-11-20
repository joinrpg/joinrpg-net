using System;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Helpers.Test
{
  [TestClass]
  public class DateInPastAttributeTest
  {
    private class ClassToValidate
    {
      [DateShouldBeInPast]
      public DateTime? Time { get; set; }

      public ClassToValidate ()
      {
        Time = DateTime.MaxValue;
      }
    }
    [TestMethod, ExpectedException(typeof(ValidationException))]
    public void TestValidationFailure()
    {
      var classToValidate = new ClassToValidate();
      var validationContext = new ValidationContext(classToValidate, null, null);
      Validator.ValidateObject(classToValidate, validationContext, true);
    }
  }
}
