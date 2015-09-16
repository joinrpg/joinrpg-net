using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public class GameObjectLinkViewModel
  {
    public GameObjectLinkType Type { get; set; }
    public string Identification { get; set; }

    public string DisplayName { get; set; }
    public int? ProjectId { get; set; }

    public bool IsGroup(int characterGroupId)
    {
      return IsObject(GameObjectLinkType.CharacterGroup, characterGroupId);
    }

    public bool IsCharacter(int characterId)
    {
      return IsObject(GameObjectLinkType.Character, characterId);
    }

    private bool IsObject(GameObjectLinkType objType, int characterGroupId)
    {
      return Type == objType && Identification == characterGroupId.ToString();
    }
  }

  public enum GameObjectLinkType
  {
    [Display(Name = "Пользователь")]
    User,
    [Display(Name = "Группа/локация")]
    CharacterGroup,
    [Display(Name = "Персонаж")]
    Character
  }
}