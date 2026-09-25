using JoinRpg.Common.WebComponents;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Portal.Controllers;

/// <summary>
/// Превращает персонажа в объект публичного JSON ролей.
/// </summary>
/// <remarks>
/// Вынесено из контроллера, чтобы правила видимости игрока проверялись юнит-тестом: ответ читают
/// анонимы через виджет встраивания ролей, и ошибка тут утекает наружу (issue #4895).
/// </remarks>
public class PublicCharacterJsonBuilder(
    IUriService uriService,
    IUriLocator<UserLinkViewModel> userLinkLocator,
    Func<CharacterViewModel, string?> claimLinkBuilder)
{
    public object Build(CharacterViewModel ch)
    {
        // Все поля игрока берём из одной ссылки: пока имя, id и ссылка вычислялись по отдельности,
        // скрытие игрока учитывалось только в ссылке, а имя и id уходили наружу (issue #4895).
        var visiblePlayer = ch.PlayerLink is { ViewMode: not ViewMode.Hide } player ? player : null;

        return new
        {
            ch.CharacterId, //TODO Remove
            CharacterLink = uriService.Get(ch),
            ch.IsAvailable,
            ch.IsFirstCopy,
            ch.CharacterName,
            Description = ch.Description?.ToHtmlString(),
            PlayerName = visiblePlayer?.DisplayName,
            PlayerId = visiblePlayer?.UserId,
            PlayerLink = visiblePlayer is null ? null : userLinkLocator.GetUri(visiblePlayer).AbsoluteUri,
            ch.ActiveClaimsCount,
            ClaimLink = ch.IsAvailable ? claimLinkBuilder(ch) : null,
        };
    }
}
