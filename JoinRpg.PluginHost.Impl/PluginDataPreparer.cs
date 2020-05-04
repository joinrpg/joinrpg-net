using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.PluginHost.Impl
{
    internal static class PluginDataPreparer
    {
        internal static CharacterInfo ToPluginModel([NotNull] this Character character)
        {
            var player = character.ApprovedClaim?.Player;
            return new CharacterInfo(
              character.CharacterName,
              character.GetFields().Select(f => f.ToPluginModel()),
              character.CharacterId,
              character.GetParentGroupsToTop().Distinct()
                .Where(g => g.IsActive && !g.IsSpecial && !g.IsRoot)
                .Select(g => g.ToPluginModel()),
              player?.GetDisplayName(), player?.FullName, player?.UserId);
        }

        private static CharacterGroupInfo ToPluginModel(this CharacterGroup g)
        {
            return new CharacterGroupInfo(g.CharacterGroupId, g.CharacterGroupName);
        }

        private static CharacterFieldInfo ToPluginModel(this FieldWithValue f)
        {
            return new CharacterFieldInfo(f.Field.ProjectFieldId, f.Value, f.Field.FieldName, f.DisplayString);
        }
    }
}
