using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.ProjectRolesLists;

internal static class ProjectRolesListViewModelBuilder
{
    public static ProjectRolesListViewModel Build(
        IReadOnlyCollection<ProjectRolesList> domainItems,
        ProjectInfo projectInfo,
        ICurrentUserAccessor currentUserAccessor,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupLinkSlimViewModel>? characterGroups = null)
    {
        var items = domainItems.Select(item => BuildListItem(item, characterGroups, projectInfo.DefaultRolesListId)).ToList();

        var hasEditAccess = projectInfo.HasEditRolesAccess(currentUserAccessor.UserIdentification);

        return new ProjectRolesListViewModel(items, hasEditAccess);
    }

    private static ProjectRolesListItemViewModel BuildListItem(
        ProjectRolesList domainItem,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupLinkSlimViewModel>? characterGroups,
        ProjectRolesListIdentification? defaultRolesListId)
    {
        CharacterGroupLinkSlimViewModel? group = null;
        if (domainItem.CharacterGroupId != null && characterGroups != null)
        {
            characterGroups.TryGetValue(domainItem.CharacterGroupId, out group);
        }

        return new ProjectRolesListItemViewModel(domainItem, group, domainItem.ProjectRolesListId == defaultRolesListId);
    }
}
