using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Money;

public class FinOperationListViewModel : IOperationsAwareView
{
    public IReadOnlyCollection<FinOperationListItemViewModel> Items { get; }

    public int ProjectId { get; }

    public IReadOnlyCollection<int> ClaimIds { get; }

    IReadOnlyCollection<int> IOperationsAwareView.CharacterIds => [];
    int? IOperationsAwareView.ProjectId => ProjectId;
    bool IOperationsAwareView.ShowCharacterCreateButton => false;

    string? IOperationsAwareView.InlineTitle => null;

    public FinOperationListViewModel(Project project, IUriService urlHelper, IReadOnlyCollection<FinanceOperation> operations)
    {
        Items = operations
          .OrderByDescending(f => f.CommentId)
          .Select(f => new FinOperationListItemViewModel(f, urlHelper))
          .ToArray();
        ProjectId = project.ProjectId;
        ClaimIds = operations.Select(c => c.ClaimId).Distinct().ToArray();
    }
}
