namespace JoinRpg.DataModel
{
  public enum LinkType
  {
    ResultUser,
    ResultCharacterGroup,
    ResultCharacter,
    Claim
  }

  public interface ILinkable
  {
    LinkType LinkType { get; }
    string Identification { get; }
    int? ProjectId { get; }
  }
}
