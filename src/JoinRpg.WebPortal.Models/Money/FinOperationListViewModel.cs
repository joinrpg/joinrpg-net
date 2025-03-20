using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Money;

public class FinOperationListViewModel(Project project, IUriService urlHelper, IReadOnlyCollection<FinanceOperation> operations) : IOperationsAwareView
{
    public IReadOnlyCollection<FinOperationListItemViewModel> Items { get; } = operations
          .OrderByDescending(f => f.CommentId)
          .Select(f => new FinOperationListItemViewModel(f, urlHelper))
          .ToArray();

    public int ProjectId { get; } = project.ProjectId;

    public IReadOnlyCollection<int> ClaimIds { get; } = [.. operations.Select(c => c.ClaimId).Distinct()];

    IReadOnlyCollection<int> IOperationsAwareView.CharacterIds => [];
    int? IOperationsAwareView.ProjectId => ProjectId;
    bool IOperationsAwareView.ShowCharacterCreateButton => false;

    string? IOperationsAwareView.InlineTitle => "Список финансовых операций";

    public string? CountString => CountHelper.DisplayCount(Items.Count, "операция", "операции", "операций");
}
