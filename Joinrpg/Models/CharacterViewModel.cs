using System.Web;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class CharacterViewModel
  {
    public int CharacterId { get; set; }
    public string CharacterName { get; set; }

    public bool IsFirstCopy { get; set; }

    public bool IsAvailable { get; set; }

    public HtmlString Description { get; set; }

    public bool IsPublic { get; set; }

    public bool IsActive { get; set; }

    public User Player { get; set; }
    public int ActiveClaimsCount { get; set; }
  }
}