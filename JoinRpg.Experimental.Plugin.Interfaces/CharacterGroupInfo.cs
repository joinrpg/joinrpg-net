namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public class CharacterGroupInfo
  {
    public CharacterGroupInfo(int characterGroupId, string characterGroupName)
    {
      CharacterGroupId = characterGroupId;
      CharacterGroupName = characterGroupName;
    }

    public int CharacterGroupId {get;}
    public string CharacterGroupName { get; }
  }
}