using System.ComponentModel.DataAnnotations;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models;

public class GameObjectLinkViewModel(IUriService uriService, ILinkableWithName worldObject)
{
    public GameObjectLinkType Type { get; } = worldObject.LinkType.AsViewModel();

    public string DisplayName { get; } = worldObject.Name;

    public bool IsActive { get; } = worldObject.IsActive;

    public Uri Uri { get; } = uriService.GetUri(worldObject);
}

public static class LinkTypeViewModelExtensions
{
    public static GameObjectLinkType AsViewModel(this LinkType type)
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
            case LinkType.Project:
                return GameObjectLinkType.Project;
            case LinkType.CommentDiscussion:
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
    Comment,
    [Display(Name = "Проект")]
    Project,
}
