using System.Text.Json.Serialization;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

/// <summary>
/// Результат запроса сетки ролей. При отсутствии доступа (непубличный список без
/// мастер-доступа) <see cref="HasAccess"/> == false и заполнен <see cref="NoAccess"/> —
/// показываем панель, а не ошибку.
/// </summary>
public record ProjectRoleGridViewResult(
    bool HasAccess,
    ProjectRoleGridViewModel? Grid,
    NoAccessToProjectViewModel? NoAccess);

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

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ProjectRoleGridCharacterRowViewModel), "character")]
[JsonDerivedType(typeof(ProjectRoleGridGroupHeaderRowViewModel), "group")]
public abstract record ProjectRoleGridRowViewModel;

public record ProjectRoleGridCharacterRowViewModel(
    CharacterLinkWithEditViewModel Character,
    PlayerCellViewModel Player,
    GroupsCellViewModel? Groups,
    IReadOnlyList<string> FieldValues) : ProjectRoleGridRowViewModel;

public record ProjectRoleGridGroupHeaderRowViewModel(
    CharacterGroupLinkSlimViewModel Group,
    string? DescriptionHtml) : ProjectRoleGridRowViewModel
{
    // Это нужно, потому что MarkupString не умеет нормально десереиализоваться из JSON
    public MarkupString? Description { get; } = DescriptionHtml is null ? null : new MarkupString(DescriptionHtml);
}

public record PlayerCellViewModel(
    CharacterApplyViewModel ApplyStatus,
    UserContacts? Contacts,
    UserLinkViewModel? Link = null);

public record GroupsCellViewModel(IReadOnlyList<CharacterGroupLinkSlimViewModel> Groups);
