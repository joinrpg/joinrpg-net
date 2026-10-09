using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.Models.ClaimList;

/// <param name="currentUserId"></param>
/// <param name="claims">EF-заявки — только ради того, чего нет в агрегате (последний комментарий).</param>
/// <param name="claimInfos">
/// Те же заявки как <see cref="ClaimInfo"/>, загруженные пачкой на весь список
/// (<c>IClaimInfoRepository.GetClaimInfos</c>), — вместе с персонажами и профилями игроков.
/// Выгрузка показывает контакты игрока, а читать их по ленивым навигациям EF на каждую строку —
/// это N+1.
/// </param>
/// <param name="roomNames">
/// Комнаты заявок по планам поселения (ADR022): заявки, которых здесь нет, не расселены.
/// </param>
public class ClaimListForExportViewModel(
    ICurrentUserAccessor currentUserId,
    IReadOnlyCollection<Claim> claims,
    IReadOnlyDictionary<ClaimIdentification, ClaimInfo> claimInfos,
    IReadOnlyDictionary<ClaimIdentification, string> roomNames)
{
    public IEnumerable<ClaimListItemForExportViewModel> Items { get; } = claims
          .Select(c => ClaimListBuilder.BuildItemForExport(
              c,
              currentUserId,
              claimInfos[c.GetId()],
              roomNames.GetValueOrDefault(c.GetId())))
          .ToList();
}
public record class ClaimListItemForExportViewModel(
    [property: Display(Name = "Имя")] string Name,
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
    [property: Display(Name = "Тип поселения")] string? AccomodationType,
    [property: Display(Name = "Комната")] string? RoomName,
    [property: Display(Name = "Льготник")] bool PreferentialFeeUser,
    [property: Display(Name = "Паспортные данные")] string? PassportData,
    [property: Display(Name = "Адрес")] string? RegistrationAddress,
    IReadOnlyDictionary<ProjectFieldIdentification, string> FieldValues,
    // Доменный UserInfo вместо EF-сущности User: выгрузке нужны имя, почта и контакты, и все они
    // приезжают загруженными пачкой, без ленивых навигаций на каждую строку.
    // Display нужен затем, что имя свойства становится префиксом заголовков колонок игрока
    // в выгрузке, а мастеру не на что смотреть в «FullPlayer».
    [property: Display(Name = "Игрок")] UserInfo FullPlayer
    ) : ILinkable
{
    #region Implementation of ILinkable

    LinkType ILinkable.LinkType => LinkType.Claim;
    string ILinkable.Identification => ClaimId.ClaimId.ToString();
    int? ILinkable.ProjectId => ClaimId.ProjectId;

    #endregion
}
