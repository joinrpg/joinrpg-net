using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

/// <summary>
/// Сетка ролей для отображения. Колонки в фиксированном порядке:
/// Персонаж → Игрок (если есть) → Группы (если есть) → Поля.
/// </summary>
public record ProjectRoleGridViewModel(
    ProjectRolesListIdentification RolesListId,
    string Name,
    string? GroupName,
    bool CanEditSettings,
    bool HasGroupsColumn,
    IReadOnlyList<string> FieldColumnNames,
    IReadOnlyList<ProjectRoleGridRowViewModel> Rows);

public record ProjectRoleGridRowViewModel(
    CharacterLinkWithEditViewModel Character,
    PlayerCellViewModel Player,
    GroupsCellViewModel? Groups,
    IReadOnlyList<string> FieldValues);

public record PlayerCellViewModel(
    CharacterApplyViewModel ApplyStatus,
    UserContacts? Contacts,
    UserLinkViewModel? Link = null);

public record GroupsCellViewModel(IReadOnlyList<CharacterGroupLinkSlimViewModel> Groups);
