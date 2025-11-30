using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Money;

public class FinOperationListViewModel(ProjectIdentification projectId, IUriService urlHelper, IReadOnlyCollection<FinanceOperation> operations) : IOperationsAwareView
{
    public IReadOnlyCollection<FinOperationListItemViewModel> Items { get; } = operations
          .OrderByDescending(f => f.CommentId)
          .Select(f => new FinOperationListItemViewModel(f, urlHelper))
          .ToArray();

    IReadOnlyCollection<ClaimIdentification> IOperationsAwareView.ClaimIds { get; } = [.. operations.Select(c => new ClaimIdentification(c.ProjectId, c.ClaimId)).Distinct()];

    IReadOnlyCollection<CharacterIdentification> IOperationsAwareView.CharacterIds => [];
    int? IOperationsAwareView.ProjectId { get; } = projectId;
    bool IOperationsAwareView.ShowCharacterCreateButton => false;

    string? IOperationsAwareView.InlineTitle => "Список финансовых операций";

    public string? CountString => CountHelper.DisplayCount(Items.Count, "операция", "операции", "операций");
}
