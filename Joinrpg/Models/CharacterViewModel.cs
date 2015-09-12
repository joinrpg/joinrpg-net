using System.Web;

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
  }
}