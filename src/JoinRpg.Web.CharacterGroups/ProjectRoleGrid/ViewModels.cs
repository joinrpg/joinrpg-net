using System.Text.Json.Serialization;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
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
/// Сетка ролей для отображения. В табличных режимах колонки в фиксированном порядке:
/// Персонаж → Игрок → Группы (если есть) → Поля. В режиме дерева поля и группы
/// показываются под именем персонажа.
/// </summary>
/// <param name="RootGroupId">Корневая группа сетки — для меню управления группой у заголовка</param>
/// <param name="SuppressFieldLabels">Не подписывать значения полей в режиме дерева
/// (выбрано единственное поле и это «Описание персонажа»)</param>
public record ProjectRoleGridViewModel(
    ProjectRolesListIdentification RolesListId,
    string Name,
    string? GroupName,
    bool CanEditSettings,
    bool HasGroupsColumn,
    IReadOnlyList<string> FieldColumnNames,
    IReadOnlyList<ProjectRoleGridRowViewModel> Rows,
    RolesGridGroupsViewMode GroupsViewMode,
    CharacterGroupIdentification RootGroupId,
    CharacterGroupType RootGroupType,
    bool SuppressFieldLabels);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ProjectRoleGridCharacterRowViewModel), "character")]
[JsonDerivedType(typeof(ProjectRoleGridGroupHeaderRowViewModel), "group")]
public abstract record ProjectRoleGridRowViewModel;

public record ProjectRoleGridCharacterRowViewModel(
    CharacterLinkWithEditViewModel Character,
    PlayerCellViewModel Player,
    GroupsCellViewModel? Groups,
    IReadOnlyList<string> FieldValues,
    CharacterGroupIdentification GroupId,
    int ActiveClaimsCount = 0,
    bool FirstCopy = true) : ProjectRoleGridRowViewModel, IMoveableListItem
{
    string IMoveableListItem.Id => Character.Character.CharacterId.ToString();
    string IMoveableListItem.ParentId => GroupId.ToString();
    string IMoveableListItem.DisplayText => Character.Character.Name;
    string IMoveableListItem.Subtext => "";
}

/// <param name="Depth">Глубина вложенности в режиме дерева (0 = корневая группа сетки)</param>
/// <param name="FirstCopy">false — группа уже встречалась выше по дереву (у другого родителя), содержимое не повторяется</param>
/// <param name="Path">Путь по дереву «А→Б→В» для всплывающей подсказки (режим дерева)</param>
/// <param name="BoundExpression">Для спецгрупп — выражение «Поле = Значение», по которому группа заполняется</param>
public record ProjectRoleGridGroupHeaderRowViewModel(
    CharacterGroupLinkSlimViewModel Group,
    string? DescriptionHtml,
    CharacterGroupType GroupType,
    int Depth = 0,
    bool FirstCopy = true,
    string? Path = null,
    string? BoundExpression = null) : ProjectRoleGridRowViewModel
{
    // Это нужно, потому что MarkupString не умеет нормально десереиализоваться из JSON
    public MarkupString? Description { get; } = DescriptionHtml is null ? null : new MarkupString(DescriptionHtml);

    public bool IsSpecial => GroupType is CharacterGroupType.SpecialToField or CharacterGroupType.SpecialToValue;
}

public record PlayerCellViewModel(
    CharacterApplyViewModel ApplyStatus,
    UserContacts? Contacts,
    UserLinkViewModel? Link = null);

public record GroupsCellViewModel(IReadOnlyList<CharacterGroupLinkSlimViewModel> Groups);
