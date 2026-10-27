using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Money;

/// <summary>
/// Снимки, нужные строкам списка операций: заявки с персонажами и игроками и авторы операций.
/// Загружаются пачкой на весь список, чтобы строка не ходила за навигациями операции.
/// </summary>
public record FinOperationListData(
    IReadOnlyDictionary<ClaimIdentification, ClaimInfo> Claims,
    IReadOnlyDictionary<UserIdentification, UserInfo> Authors);

public class FinOperationListViewModel(
    ProjectInfo projectInfo,
    IUriService urlHelper,
    IReadOnlyCollection<FinanceOperation> operations,
    FinOperationListData data,
    UserIdentification? currentUser) : IOperationsAwareView
{
    public IReadOnlyCollection<FinOperationListItemViewModel> Items { get; } = operations
          .OrderByDescending(f => f.CommentId)
          .Select(f => new FinOperationListItemViewModel(
              f,
              data.Claims[new ClaimIdentification(f.ProjectId, f.ClaimId)],
              data.Authors[new UserIdentification(f.Comment.AuthorUserId)],
              urlHelper,
              projectInfo,
              currentUser))
          .ToArray();

    IReadOnlyCollection<ClaimIdentification> IOperationsAwareView.ClaimIds { get; } = [.. operations.Select(c => new ClaimIdentification(c.ProjectId, c.ClaimId)).Distinct()];

    IReadOnlyCollection<CharacterIdentification> IOperationsAwareView.CharacterIds => [];
    int? IOperationsAwareView.ProjectId { get; } = projectInfo.ProjectId;
    bool IOperationsAwareView.ShowCharacterCreateButton => false;

    string? IOperationsAwareView.InlineTitle => "Список финансовых операций";

    public string? CountString => CountHelper.DisplayCount(Items.Count, "операция", "операции", "операций");
}
