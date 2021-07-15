using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.ClaimList
{
    public class ClaimListItemViewModel : ClaimListItemViewModelBase
    {

        [Display(Name = "Проблема")]
        public ICollection<ProblemViewModel> Problems { get; }
        public int UnreadCommentsCount { get; }

        public ClaimListItemViewModel(Claim claim, int currentUserId, int unreadCommentsCount, IEnumerable<ClaimProblem> problem) : base(claim, currentUserId)
        {
            UnreadCommentsCount = unreadCommentsCount;
            Problems = problem.Select(p => new ProblemViewModel(p)).ToList();
        }
    }
}
