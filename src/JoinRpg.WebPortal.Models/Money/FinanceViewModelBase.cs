using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models;

public class FinanceViewModelBase : AddCommentViewModel
{
    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateOnly OperationDate { get; set; }

    [ReadOnly(true)]
    public bool ClaimApproved { get; set; }

    public int ClaimId { get; set; }
}
