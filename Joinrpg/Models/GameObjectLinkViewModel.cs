using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
  public class GameObjectLinkViewModel
  {
    public GameObjectLinkType Type { get; set; }
    public string Identification { get; set; }

    public string DisplayName { get; set; }
    public int? ProjectId { get; set; }
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