using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.Models.ClaimList;

/// <summary>
/// Use this ctor if need to export claims from different projects
/// </summary>
/// <param name="currentUserId"></param>
/// <param name="claimPair"></param>
/// <param name="players">
/// Профили игроков, загруженные пачкой на весь список (<c>IUserRepository.GetRequiredUserInfos</c>).
/// Выгрузка показывает контакты игрока, а читать их по ленивым навигациям EF на каждую строку —
/// это N+1.
/// </param>
/// <param name="roomNames">
/// Комнаты заявок по планам поселения (ADR022): заявки, которых здесь нет, не расселены.
/// </param>
public class ClaimListForExportViewModel(
    ICurrentUserAccessor currentUserId,
    IReadOnlyCollection<(Claim Claim, ProjectInfo ProjectInfo)> claimPair,
    IReadOnlyDictionary<UserIdentification, UserInfo> players,
    IReadOnlyDictionary<ClaimIdentification, string> roomNames)
{
    public IEnumerable<ClaimListItemForExportViewModel> Items { get; } = claimPair
          .Select(c => ClaimListBuilder.BuildItemForExport(
              c.Claim,
              currentUserId,
              c.ProjectInfo,
              players[c.Claim.GetPlayerId()],
              roomNames.GetValueOrDefault(c.Claim.GetId())))
          .ToList();

    public ClaimListForExportViewModel(
        ICurrentUserAccessor currentUserId,
        IReadOnlyCollection<Claim> claims,
        ProjectInfo projectInfo,
        IReadOnlyDictionary<UserIdentification, UserInfo> players,
        IReadOnlyDictionary<ClaimIdentification, string> roomNames)
        : this(currentUserId, [.. claims.Select(c => (c, projectInfo))], players, roomNames)
    {
    }
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
