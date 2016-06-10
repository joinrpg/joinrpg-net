﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  /// <summary>
  /// CharacterInfo is locked down version of character
  /// </summary>
  public class CharacterInfo
  {
    public CharacterInfo([NotNull] string characterName, [NotNull] IEnumerable<CharacterFieldInfo> fields, int characterId, IEnumerable<CharacterGroupInfo> groups)
    {
      if (characterName == null) throw new ArgumentNullException(nameof(characterName));
      if (fields == null) throw new ArgumentNullException(nameof(fields));
      CharacterName = characterName;
      Fields = fields.ToArray();
      CharacterId = characterId;
      Groups = groups.ToArray();
    }

    public int CharacterId { get; }

    [NotNull]
    public string CharacterName { get; }

    [NotNull, ItemNotNull]
    public IEnumerable<CharacterFieldInfo> Fields { get; }

    [NotNull, ItemNotNull]
    public IEnumerable<CharacterGroupInfo> Groups { get; }
  }
}
