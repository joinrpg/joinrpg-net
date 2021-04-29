using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
    public static class ResponsibleMasterExtensions
    {
        [NotNull, ItemNotNull]
        public static IEnumerable<User> GetResponsibleMasters([NotNull] this IClaimSource group, bool includeSelf = true)
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

        [CanBeNull]
        public static User? GetResponsibleMaster([NotNull] this Character character)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            return character.ApprovedClaim?.ResponsibleMasterUser ?? character.GetResponsibleMasters().FirstOrDefault();
        }
    }
}
