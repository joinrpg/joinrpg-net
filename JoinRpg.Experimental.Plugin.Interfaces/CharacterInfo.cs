using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  /// <summary>
  /// CharacterInfo is locked down version of character
  /// </summary>
  public class CharacterInfo
  {
    public CharacterInfo([NotNull] string characterName, [NotNull] IEnumerable<CharacterFieldInfo> fields)
    {
      if (characterName == null) throw new ArgumentNullException(nameof(characterName));
      if (fields == null) throw new ArgumentNullException(nameof(fields));
      CharacterName = characterName;
      Fields = fields;
    }

    [NotNull]
    public string CharacterName { get; }

    [NotNull, ItemNotNull]
    public IEnumerable<CharacterFieldInfo> Fields { get; }
  }
}
