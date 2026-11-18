namespace JoinRpg.Web.ProjectMasterTools.Acl;

/// <summary>Мастер в списке перестановки (ADR019, §5): кого двигать и как показать в диалоге «переместить после».</summary>
/// <remarks>
/// <see cref="IMoveableListItem"/> реализован явно: модель ездит в остров по JSON, а явные члены интерфейса
/// System.Text.Json не сериализует — в параметрах острова остаются только MasterId, DisplayName и Role.
/// </remarks>
public record MasterMoveItemViewModel(ProjectMasterIdentification MasterId, string DisplayName, string Role) : IMoveableListItem
{
    string IMoveableListItem.Id => MasterId.ToString();
    string IMoveableListItem.ParentId => MasterId.ProjectId.ToString();
    string IMoveableListItem.DisplayText => DisplayName;
    string IMoveableListItem.Subtext => Role;
}
