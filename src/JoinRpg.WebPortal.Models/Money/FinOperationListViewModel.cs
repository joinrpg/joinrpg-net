using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using MoreLinq;

namespace JoinRpg.Web.Models
{
    public class FinOperationListViewModel : IOperationsAwareView
    {
        public IReadOnlyCollection<FinOperationListItemViewModel> Items { get; }

        public int? ProjectId { get; }

        public IReadOnlyCollection<int> ClaimIds { get; }
        public IReadOnlyCollection<int> CharacterIds => Array.Empty<int>();

        bool IOperationsAwareView.ShowCharacterCreateButton => false;

        public FinOperationListViewModel(Project project, IUriService urlHelper, IReadOnlyCollection<FinanceOperation> operations)
        {
            Items = operations
              .OrderBy(f => f.CommentId)
              .Select(f => new FinOperationListItemViewModel(f, urlHelper)).ToArray();
            ProjectId = project.ProjectId;
            ClaimIds = operations.Select(c => c.ClaimId).Distinct().ToArray();
        }
    }
}
