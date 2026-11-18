using System.ComponentModel.DataAnnotations;

namespace JoinRpg.WebPortal.Models.Test;

public class UnblockProjectViewModelTest
{
    [Theory]
    [InlineData("все восстановлено")]
    [InlineData("  Всё Восстановлено ")]
    [InlineData("ВСЁ ВОССТАНОВЛЕНО")]
    public void AcceptsConfirmationPhrase(string confirmation)
        => UnblockProjectViewModel.IsConfirmationPhraseValid(confirmation).ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("разблокировать")]
    [InlineData("все восстановлен")]
    public void RejectsOtherInput(string? confirmation)
        => UnblockProjectViewModel.IsConfirmationPhraseValid(confirmation).ShouldBeFalse();

    [Fact]
    public void ValidationErrorIsBoundToConfirmationField()
    {
        var model = new UnblockProjectViewModel { Confirmation = "да" };

        var results = model.Validate(new ValidationContext(model)).ToList();

        results.ShouldHaveSingleItem().MemberNames.ShouldBe([nameof(UnblockProjectViewModel.Confirmation)]);
    }

    [Fact]
    public void NoValidationErrorWithCorrectPhrase()
    {
        var model = new UnblockProjectViewModel { Confirmation = "Всё восстановлено" };

        model.Validate(new ValidationContext(model)).ShouldBeEmpty();
    }
}
