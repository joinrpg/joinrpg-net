namespace JoinRpg.Web.ProjectCommon;

/// <summary>
/// Ссылка на персонажа с дополнительными мастерскими действиями: редактирование
/// персонажа (при наличии права) и переход на принятую заявку (если она есть).
/// </summary>
public record CharacterLinkWithEditViewModel(
    CharacterLinkSlimViewModel Character,
    bool CanEdit,
    ClaimIdentification? ApprovedClaimId);
