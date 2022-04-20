using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models
{
    /// <summary>
    /// Used in preferential fee request
    /// </summary>
    public class MarkMeAsPreferentialViewModel : FinanceViewModelBase
    {
        public JoinHtmlString PreferentialFeeConditions { get; set; }

        public MarkMeAsPreferentialViewModel() => ShowLabel = false;

        public MarkMeAsPreferentialViewModel(ClaimViewModel claim) : this()
        {
            ProjectId = claim.ProjectId;
            ClaimId = claim.ClaimId;
            OperationDate = DateTime.UtcNow;
            CommentDiscussionId = claim.CommentDiscussionId;
            PreferentialFeeConditions = claim.ClaimFee.PreferentialFeeConditions;
        }
    }
}
