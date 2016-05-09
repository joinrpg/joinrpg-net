using System;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class GameObjectLinkViewModel : ILinkable
  {
    public LinkType LinkType { get; set; }
    public GameObjectLinkType Type => ConvertToLinkType(LinkType);
    public string Identification { get; set; }

    public string DisplayName { get; set; }
    public int? ProjectId { get; set; }

    public bool IsActive { get; set; }


    private static GameObjectLinkType ConvertToLinkType(LinkType type)
    {
      switch (type)
      {
        case LinkType.ResultUser:
        return GameObjectLinkType.User;
        case LinkType.ResultCharacterGroup:
        return GameObjectLinkType.CharacterGroup;
        case LinkType.ResultCharacter:
        return GameObjectLinkType.Character;
        case LinkType.Plot:
          return GameObjectLinkType.Plot;
        case LinkType.Claim:
          return GameObjectLinkType.Claim;
        case LinkType.Comment:
          return GameObjectLinkType.Comment;
        default:
        throw new ArgumentOutOfRangeException(nameof(type), type, null);
      }
    }
  }

  public enum GameObjectLinkType
  {
    [Display(Name = "Пользователь")]
    User,
    [Display(Name = "Группа ролей")]
    CharacterGroup,
    [Display(Name = "Персонаж/роль")]
    Character,
    [Display(Name = "Заявка")]
    Claim,
    [Display(Name = "Сюжет")]
    Plot,
    Comment
  }
}