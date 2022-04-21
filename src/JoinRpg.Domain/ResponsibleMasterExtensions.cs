using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain;

public static class ResponsibleMasterExtensions
{
    private static IEnumerable<User> GetResponsibleMasters(this IClaimSource group, bool includeSelf = true)
    {
        if (group == null)
        {
            throw new ArgumentNullException(nameof(group));
        }

        if (group.ResponsibleMasterUser != null && includeSelf)
        {
            return new[] { group.ResponsibleMasterUser };
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
        return candidates.Except(removedGroups).Select(c => c.ResponsibleMasterUser);
    }

    public static User? GetResponsibleMasterOrDefault(this Character character)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        return character.ApprovedClaim?.ResponsibleMasterUser ?? character.GetResponsibleMasters().FirstOrDefault();
    }

    public static User GetResponsibleMaster(this IClaimSource source)
    {
        var responsibleMaster = source.GetResponsibleMasters().FirstOrDefault()
            //if we failed to calculate responsible master, assign owner as responsible master
            ?? source.Project.ProjectAcls.Where(w => w.IsOwner).FirstOrDefault()?.User
            //if we found no owner, assign random master
            ?? source.Project.ProjectAcls.First().User;
        return responsibleMaster;
    }
}
