using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.ClaimList
{
    public class ClaimListItemViewModel : ClaimListItemViewModelBase
    {

        [Display(Name = "Проблема")]
        public ICollection<ProblemViewModel> Problems { get; set; }
        public int UnreadCommentsCount { get; }

        public ClaimListItemViewModel(Claim claim, int currentUserId) : base(claim, currentUserId)
        {
            UnreadCommentsCount = claim.CommentDiscussion.GetUnreadCount(currentUserId);
        }


        public ClaimListItemViewModel AddProblems(IEnumerable<ClaimProblem> problem)
        {
            Problems =
                problem.Select(p => new ProblemViewModel(p)).ToList();
            return this;
        }
    }
}
