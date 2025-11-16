using System.ComponentModel.DataAnnotations;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims.Finances;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Claims.UnifiedGrid;

public record class ClaimListItemViewModel(
    [property: Display(Name = "Персонаж")] string Name,
    [property: Display(Name = "Игрок")] UserLinkViewModel Player,
    [property: Display(Name = "Игра")] string ProjectName,
    [property: Display(Name = "Статус")] ClaimFullStatusView ClaimFullStatusView,
    [property: Display(Name = "Обновлена"), UIHint("EventTime")] DateTime? UpdateDate,
    [property: Display(Name = "Создана"), UIHint("EventTime")] DateTime? CreateDate,
    [property: Display(Name = "Дата заезда")] DateTime? CheckInDate,
    [property: Display(Name = "Ответственный")] UserLinkViewModel Responsible,
    [property: Display(Name = "Уплачено")] int FeePaid,
    [property: Display(Name = "Осталось")] int FeeDue,
    [property: Display(Name = "Итого взнос")] int TotalFee,
    UserLinkViewModel? LastModifiedBy,
    ClaimIdentification ClaimId,
    [property: Display(Name = "Проблема")] IReadOnlyCollection<ProblemViewModel> Problems,
    int UnreadCommentsCount,
    string PlayerFullName
    ) : ILinkable
{
    #region Implementation of ILinkable

    LinkType ILinkable.LinkType => LinkType.Claim;
    string ILinkable.Identification => ClaimId.ClaimId.ToString();
    int? ILinkable.ProjectId => ClaimId.ProjectId;

    #endregion
}

public record class UgItemForCaptainViewModel(
    UgCharacterForCaptainViewModel Character,
    UgClaimForCaptainViewModel[] Claims
    )
{

}

public record class UgCharacterForCaptainViewModel(
    [property: Display(Name = "Персонаж")] CharacterLinkSlimViewModel Name,
    CharacterBusyStatusView BusyStatus,
    int? SlotCount,
    IReadOnlyCollection<CharacterGroupLinkSlimViewModel> Groups,
    bool IsHot);

public record class UgClaimForCaptainViewModel(
     [property: Display(Name = "Игрок")] UserLinkViewModel? Player,
    [property: Display(Name = "Статус заявки")] ClaimFullStatusView ClaimStatus,
    [property: Display(Name = "Обновлена"), UIHint("EventTime")] DateTimeOffset? UpdateDate,
    [property: Display(Name = "Создана"), UIHint("EventTime")] DateTimeOffset? CreateDate,
    [property: Display(Name = "Дата заезда")] DateTimeOffset? CheckInDate,
    [property: Display(Name = "Ответственный")] UserLinkViewModel? Responsible,
    [property: Display(Name = "Взнос")] ClaimBalance Fee,
    ClaimIdentification? ClaimId,
    string? PlayerFullName);
