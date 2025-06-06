using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers.Validation;

namespace JoinRpg.Helpers.Test;

public class DateInPastAttributeTest
{
    private class ClassToValidateInPast
    {
        [DateShouldBeInPast]
        public DateTime? Time { get; } = DateTime.MaxValue;
    }

    private class ClassToValidateEmpty
    {
        [CannotBeEmpty]
        public IEnumerable<int> List { get; } = new List<int>();
    }

    [Fact]
    public void TestShouldBeInPastFailure() => Should.Throw<ValidationException>(() => Validate(new ClassToValidateInPast()));

    private static void Validate(object classToValidate)
    {
        var validationContext = new ValidationContext(classToValidate, null, null);
        Validator.ValidateObject(classToValidate, validationContext, true);
    }

    [Fact]
    public void TestCantBeEmptyFailure() => Should.Throw<ValidationException>(() => Validate(new ClassToValidateEmpty()));
}
