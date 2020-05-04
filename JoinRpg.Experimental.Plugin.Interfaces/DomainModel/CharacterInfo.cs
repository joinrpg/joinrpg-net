using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
    /// <summary>
    /// CharacterInfo is locked down version of character
    /// </summary>
    [PublicAPI]
    public class CharacterInfo
    {
        public CharacterInfo([NotNull] string characterName, [NotNull] IEnumerable<CharacterFieldInfo> fields,
          int characterId, IEnumerable<CharacterGroupInfo> groups, [CanBeNull] string playerName,
          [CanBeNull] string playerFullName, int? playerId)
        {
            if (characterName == null) throw new ArgumentNullException(nameof(characterName));
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            CharacterName = characterName;
            Fields = fields.ToArray();
            CharacterId = characterId;
            PlayerName = playerName;
            PlayerFullName = playerFullName;
            Groups = groups.ToArray();
            PlayerId = playerId;
        }

        public int CharacterId { get; }

        [NotNull]
        public string CharacterName { get; }

        [NotNull, ItemNotNull]
        public IEnumerable<CharacterFieldInfo> Fields { get; }

        [NotNull, ItemNotNull]
        public IEnumerable<CharacterGroupInfo> Groups { get; }

        //TODO: Move all this into PlayerInfo
        [CanBeNull]
        public string PlayerName { get; }

        [CanBeNull]
        public string PlayerFullName { get; }

        public bool HasPlayer => PlayerId != null;

        [CanBeNull]
        public int? PlayerId { get; }
    }
}
