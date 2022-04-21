using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models;

public class FinanceViewModelBase : AddCommentViewModel
{
    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate { get; set; }

    [ReadOnly(true)]
    public bool ClaimApproved { get; set; }

    public int ClaimId { get; set; }
}
