using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.CharacterGroups;

public enum GroupNavigationPage
{
    None,
    Home,
    Roles,
    ClaimsActive,
    ClaimsDiscussing,
    Characters,
    Report,
    Forums,
    Plots,
}

public class CharacterGroupDetailsViewModel(
    CharacterGroupFullInfo group,
    ProjectInfo projectInfo,
    UserIdentification? currentUser,
    GroupNavigationPage page) : CharacterGroupWithDescViewModel(group)
{
    public GroupNavigationPage Page { get; } = page;

    public bool HasMasterAccess { get; } = projectInfo.HasMasterAccess(currentUser);
    /// <summary>Добавление персонажа и удаление группы (оно перевешивает персонажей) — только в активном проекте.</summary>
    public bool ShowEditControls { get; } = projectInfo.HasEditRolesAccess(currentUser);
    /// <summary>Настройка группы и подгруппы — и в заблокированном проекте (ADR023).</summary>
    public bool ShowEditGroupControls { get; } = projectInfo.HasEditGroupsAccess(currentUser);
    public bool IsSpecial { get; } = group.IsSpecial;
    public bool IsRootGroup { get; } = group.IsRoot;
    public CreateUpdateMarksViewModel Marks { get; } = group.Marks.ToViewModel();
}
