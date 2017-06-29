using System;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public class CharacterGroupInfo : IEquatable<CharacterGroupInfo>
  {
    public CharacterGroupInfo(int characterGroupId, string characterGroupName)
    {
      CharacterGroupId = characterGroupId;
      CharacterGroupName = characterGroupName;
    }

    public int CharacterGroupId { get; }
    public string CharacterGroupName { get; }

    public bool Equals(CharacterGroupInfo other) => other != null && other.CharacterGroupId == this.CharacterGroupId;

    public override string ToString() => CharacterGroupName;
  }
}