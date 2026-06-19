using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;

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
    bool HasPlayerColumn,
    bool HasGroupsColumn,
    IReadOnlyList<string> FieldColumnNames,
    IReadOnlyList<ProjectRoleGridRowViewModel> Rows);

public record ProjectRoleGridRowViewModel(
    CharacterLinkSlimViewModel Character,
    PlayerCellViewModel? Player,
    GroupsCellViewModel? Groups,
    IReadOnlyList<string> FieldValues);

/// <summary>Имя/статус игрока («NPC», «нет игрока», имя) + контакты (или null).</summary>
public record PlayerCellViewModel(string Name, UserContacts? Contacts);

public record GroupsCellViewModel(IReadOnlyList<CharacterGroupLinkSlimViewModel> Groups);
