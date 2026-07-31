using System.ComponentModel.DataAnnotations;

namespace JoinRpg.WebPortal.Models.Test;

public class ClaimOperationViewModelTest
{
    [Fact]
    public void ApproveClaimOperation_AllowsEmptyComment()
    {
        var vm = new ClaimOperationViewModel
        {
            ActionName = "Принять заявку",
            CommentText = "",
        };

        var results = Validate(vm);

        results.ShouldBeEmpty();
    }

    [Fact]
    public void OnHoldClaimOperation_RequiresComment()
    {
        var vm = new RequiredCommentClaimOperationViewModel
        {
            ActionName = "В лист ожидания",
            CommentText = "",
        };

        var results = Validate(vm);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(ClaimOperationViewModel.CommentText)));
    }

    [Fact]
    public void OnHoldClaimOperation_ValidWithComment()
    {
        var vm = new RequiredCommentClaimOperationViewModel
        {
            ActionName = "В лист ожидания",
            CommentText = "Комментарий",
        };

        var results = Validate(vm);

        results.ShouldBeEmpty();
    }

    [Fact]
    public void MasterDeclineOperation_RequiresComment()
    {
        var vm = new MasterDenialOperationViewModel
        {
            ActionName = "Отклонить заявку",
            CommentText = "",
            DenialStatus = ClaimDenialStatusView.Unavailable,
        };

        var results = Validate(vm);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(ClaimOperationViewModel.CommentText)));
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
