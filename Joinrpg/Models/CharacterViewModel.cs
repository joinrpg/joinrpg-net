using System.Web;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class CharacterViewModel  : ICharacterWithPlayerViewModel
  {
    public int CharacterId { get; set; }
    public int ProjectId { get; set; }
    public string CharacterName { get; set; }

    public bool IsFirstCopy { get; set; }

    public bool IsAvailable { get; set; }

    public HtmlString Description { get; set; }

    public bool IsPublic { get; set; }

    public bool IsActive { get; set; }

    public User Player { get; set; }
    public bool HidePlayer { get; set; }
    public bool HasAccess => HasMasterAccess;
    public int ActiveClaimsCount { get; set; }

    public bool HasMasterAccess { get; set; }

    public bool FirstInGroup { get; set; }
    public bool LastInGroup { get; set; }

    public int ParentCharacterGroupId { get; set; }
    public int RootGroupId { get; set; }
  }
}