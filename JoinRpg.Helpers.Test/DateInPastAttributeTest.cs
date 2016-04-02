using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.Helpers.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Helpers.Test
{
  [TestClass]
  public class DateInPastAttributeTest
  {
    private class ClassToValidateInPast
    {
      [DateShouldBeInPast, UsedImplicitly]
      public DateTime? Time { get; } = DateTime.MaxValue;
    }

    private class ClassToValidateEmpty
    {
      [CannotBeEmpty, UsedImplicitly]
      public IEnumerable<int> List { get; } = new List<int>();
    }

    [TestMethod, ExpectedException(typeof(ValidationException))]
    public void TestShouldBeInPastFailure()
    {
      Validate(new ClassToValidateInPast());
    }

    private static void Validate(object classToValidate)
    {
      var validationContext = new ValidationContext(classToValidate, null, null);
      Validator.ValidateObject(classToValidate, validationContext, true);
    }

    [TestMethod, ExpectedException(typeof(ValidationException))]
    public void TestCantBeEmptyFailure()
    {
      Validate(new ClassToValidateEmpty());
    }
  }
}
