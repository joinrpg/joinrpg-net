using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
    public class PaymentViewModelBase : FinanceViewModelBase
    {
        [Display(Name = "Внесено денег"), Required]
        public int Money { get; set; }

        public int FeeChange { get; set; }

        [Display(Name = "Кому и как оплачено"), Required]
        public int PaymentTypeId { get; set; }

        [ReadOnly(true)]
        public bool HasUnApprovedPayments { get; set; }

        public PaymentViewModelBase() { }

        public PaymentViewModelBase(ClaimViewModel claim)
        {
            CommentText = "";
            ProjectId = claim.ProjectId;
            CommentDiscussionId = claim.ClaimId;
            ClaimId = claim.ClaimId;
            ParentCommentId = null;
            EnableHideFromUser = false;
            HideFromUser = false;
            OperationDate = DateTime.Today;
            Money = Math.Max(claim.ClaimFee.CurrentFee - claim.ClaimFee.CurrentBalance, 0);
            ClaimApproved = claim.Status.IsAlreadyApproved();

            // TODO: Probably must get this info from the list of finance operations?
            HasUnApprovedPayments = claim.HasMasterAccess &&
                claim.RootComments.Any(c => c.ShowFinanceModeration);
        }
    }
}
