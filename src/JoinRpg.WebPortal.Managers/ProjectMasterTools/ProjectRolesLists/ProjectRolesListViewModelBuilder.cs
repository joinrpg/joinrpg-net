using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.ProjectRolesLists;

internal static class ProjectRolesListViewModelBuilder
{
    public static ProjectRolesListViewModel Build(
        IReadOnlyCollection<ProjectRolesList> domainItems,
        ProjectInfo projectInfo,
        ICurrentUserAccessor currentUserAccessor,
        IReadOnlyDictionary<CharacterGroupIdentification, string>? characterGroupNames = null)
    {
        var items = domainItems.Select(item => BuildListItem(item, characterGroupNames)).ToList();

        var hasEditAccess = projectInfo.HasEditRolesAccess(currentUserAccessor.UserIdentification);

        return new ProjectRolesListViewModel(items, hasEditAccess);
    }

    private static ProjectRolesListItemViewModel BuildListItem(
        ProjectRolesList domainItem,
        IReadOnlyDictionary<CharacterGroupIdentification, string>? characterGroupNames)
    {
        string? groupName = null;
        if (domainItem.CharacterGroupId != null && characterGroupNames != null)
        {
            characterGroupNames.TryGetValue(domainItem.CharacterGroupId, out groupName);
        }

        return new ProjectRolesListItemViewModel(domainItem, groupName);
    }
}
