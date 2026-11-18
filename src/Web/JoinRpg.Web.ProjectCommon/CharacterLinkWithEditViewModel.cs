namespace JoinRpg.Web.ProjectCommon;

/// <summary>
/// Ссылка на персонажа с дополнительными мастерскими действиями: редактирование
/// персонажа (при наличии права) и переход на заявку (если она передана).
/// </summary>
/// <param name="ClaimId">Заявка, на которую ведёт иконка; какую заявку показывать (например, только принятую) решает вызывающий/сервер.</param>
public record CharacterLinkWithEditViewModel(
    CharacterLinkSlimViewModel Character,
    bool CanEdit,
    ClaimIdentification? ClaimId);
