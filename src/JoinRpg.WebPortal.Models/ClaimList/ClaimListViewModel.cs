using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models.ClaimList
{
    public class ClaimListViewModel : IOperationsAwareView
    {
        public IEnumerable<ClaimListItemViewModel> Items { get; }

        public int? ProjectId { get; }
        public IReadOnlyCollection<int> ClaimIds { get; }
        public IReadOnlyCollection<int> CharacterIds { get; }

        public bool ShowCount { get; }
        public bool ShowUserColumn { get; }

        public ClaimListViewModel(
            int currentUserId,
            IReadOnlyCollection<Claim> claims,
            int? projectId,
            Dictionary<int, int> unreadComments,
            bool showCount = true,
            bool showUserColumn = true
            )
        {
            Items = claims
              .Select(c => new ClaimListItemViewModel(c, currentUserId, unreadComments.GetValueOrDefault(c.CommentDiscussionId), c.GetProblems()))
              .ToList();
            ClaimIds = claims.Select(c => c.ClaimId).ToArray();
            CharacterIds = claims.Select(c => c.CharacterId).WhereNotNull().ToArray();
            ProjectId = projectId;
            ShowCount = showCount;
            ShowUserColumn = showUserColumn;
        }
    }
}
