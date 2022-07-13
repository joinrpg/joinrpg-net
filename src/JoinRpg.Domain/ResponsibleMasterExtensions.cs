using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain;

public static class ResponsibleMasterExtensions
{
    private static User? SelectResponsibleMaster(this IClaimSource group, bool includeSelf = true)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (group.ResponsibleMasterUser != null && includeSelf)
        {
            return group.ResponsibleMasterUser;
        }
        var candidates = new HashSet<CharacterGroup>();
        var removedGroups = new HashSet<CharacterGroup>();
        var lookupGroups = new HashSet<CharacterGroup>(group.ParentGroups);
        while (lookupGroups.Any())
        {
            var currentGroup = lookupGroups.First();
            _ = lookupGroups.Remove(currentGroup); //Get next group

            if (removedGroups.Contains(currentGroup) || candidates.Contains(currentGroup))
            {
                continue;
            }

            if (currentGroup.ResponsibleMasterUserId != null)
            {
                _ = candidates.Add(currentGroup);
                removedGroups.UnionWith(currentGroup.FlatTree(c => c.ParentGroups, includeSelf: false));
                //Some group with set responsible master will shadow out all parents.
            }
            else
            {
                lookupGroups.UnionWith(currentGroup.ParentGroups);
            }
        }
        return candidates
            .Except(removedGroups)
            .OrderBy(g => g.CharacterGroupId) // It ensures consistent order
            .Select(c => c.ResponsibleMasterUser)
            .WhereNotNull() // It's not required, because we only add groups
                            // with RespMaster not null. But it will make compiler happy
            .FirstOrDefault();
    }

    public static User? GetResponsibleMasterOrDefault(this Character character)
    {
        ArgumentNullException.ThrowIfNull(character);

        return
            character.ApprovedClaim?.ResponsibleMasterUser
            ?? character.SelectResponsibleMaster();
    }

    public static User GetResponsibleMaster(this IClaimSource source)
    {
        return
            source.SelectResponsibleMaster()
            ?? source.Project.GetDefaultResponsibleMaster();
    }

    public static User GetDefaultResponsibleMaster(this Project project)
    {

        return
            project.RootGroup.ResponsibleMasterUser
            //if we failed to calculate responsible master, assign owner as responsible master
            ?? project.ProjectAcls.Where(w => w.IsOwner).FirstOrDefault()?.User
            //if we found no owner, assign random (but consistent) master
            ?? project.ProjectAcls.OrderBy(u => u.UserId).First().User;
    }
}
