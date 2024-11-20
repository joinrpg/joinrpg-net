using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Characters;

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

public class CharacterGroupDetailsViewModel(CharacterGroup group, int? currentUser, GroupNavigationPage page) : CharacterGroupWithDescViewModel(group), ICreatedUpdatedTracked
{
    public GroupNavigationPage Page { get; } = page;

    public bool HasMasterAccess { get; } = group.HasMasterAccess(currentUser);
    public bool ShowEditControls { get; } = group.HasEditRolesAccess(currentUser);
    public bool IsSpecial { get; } = group.IsSpecial;
    public bool IsRootGroup { get; } = group.IsRoot;
    public DateTime CreatedAt { get; } = group.CreatedAt;
    public User CreatedBy { get; } = group.CreatedBy;
    public DateTime UpdatedAt { get; } = group.UpdatedAt;
    public User UpdatedBy { get; } = group.UpdatedBy;
}
