using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models.Accommodation;

public class AccRequestViewModel
{
    public int Id { get; protected set; }

    [JsonIgnore]
    public int ProjectId { get; protected set; }

    [JsonIgnore]
    public int AccommodationTypeId { get; protected set; }

    /// <summary>
    /// Комната, в которую расселена группа, или <c>0</c>, если она ещё не расселена.
    /// </summary>
    /// <remarks>
    /// Ноль здесь — часть контракта с <c>wwwroot/Scripts/rooms.js</c>: модель уезжает в разметку
    /// как JSON (атрибут <c>requests</c> у строки комнаты и переменная <c>requestsNotAssigned</c>),
    /// а скрипт расселяет и выселяет на клиенте, проверяя <c>req.RoomId &gt; 0</c> и сбрасывая
    /// поле в <c>0</c> при выселении. Поэтому тип остаётся <c>int</c>, а не nullable.
    /// </remarks>
    public int RoomId { get; protected set; }

    [JsonIgnore]
    public IReadOnlyList<RequestParticipantViewModel> Participants { get; protected set; }

    public int Persons
        => Participants?.Count ?? 0;

    public string PersonsList => Participants.Select(p => p.UserName).JoinStrings(@", ");

    public object? Instance => null;

    public int FeeTotal { get; protected set; }

    public int FeeToPay { get; protected set; }

    public string PaymentStatusCssClass { get; protected set; }

    public string PaymentStatusTitle
        => FeeToPay > 0 ? $@"Не оплачено {FeeToPay} из {FeeTotal}" : "Все оплачено полностью";

    /// <summary>
    /// Группа проживающих поверх доменного агрегата плана поселения (ADR018).
    /// </summary>
    /// <param name="group">Группа из плана — она несёт только идентификаторы жильцов</param>
    /// <param name="participants">
    /// Уже посчитанные жильцы: деньги в план не входят (ADR018, §5), поэтому они приходят снаружи,
    /// из общей на всю страницу выборки персонажей.
    /// </param>
    public AccRequestViewModel(AccommodationGroupInfo group, IReadOnlyList<RequestParticipantViewModel> participants)
    {
        Id = group.Id.AccommodationRequestId;
        ProjectId = group.Id.ProjectId.Value;
        AccommodationTypeId = group.AccommodationTypeId.AccommodationTypeId;
        // RoomId у группы плана равен null ровно тогда, когда она ещё не расселена по комнатам
        // (такие группы план отдаёт в UnassignedGroups). Вью-модель кодирует это нулём — см. RoomId.
        RoomId = group.RoomId?.RoomId ?? 0;
        Participants = participants;
        FeeTotal = Participants.Sum(p => p.FeeTotal);
        FeeToPay = Participants.Sum(p => p.FeeToPay);
        FeeToPay = FeeToPay > 0 ? FeeToPay : 0; // if FeeToPay < 0 we have overpaid
        var percent = FeeToPay == 0 ? 100 : (FeeToPay == FeeTotal ? 0 : 100 * FeeToPay / FeeTotal);
        PaymentStatusCssClass = percent == 100 ? @"success" : (percent >= 25 ? @"warning" : @"danger");
    }
}

public class RequestParticipantViewModel
{
    public ClaimIdentification ClaimId { get; }

    public UserIdentification UserId { get; }

    public int FeeToPay { get; }

    public int FeeTotal { get; }

    public string UserName { get; }

    /// <summary>
    /// Жилец поверх доменного агрегата персонажа (ADR013): и имя игрока, и баланс заявки берутся
    /// из него, без обращения к EF-сущностям.
    /// </summary>
    public RequestParticipantViewModel(CharacterInfo character, CharacterClaimInfo claim, ProjectInfo projectInfo)
    {
        ClaimId = claim.ClaimId;
        UserId = claim.PlayerId;
        UserName = claim.Player.DisplayName.DisplayName;
        var balance = character.CalculateClaimBalance(claim, projectInfo);
        FeeTotal = balance.TotalFee;
        FeeToPay = balance.FeeDue;
    }

    /// <summary>
    /// Жилец из готовых значений — для тестов формы JSON (см. <see cref="AccRequestJson"/>).
    /// Собирать ради проверки имён свойств доменный агрегат персонажа с метаданными проекта незачем.
    /// </summary>
    internal RequestParticipantViewModel(
        ClaimIdentification claimId,
        UserIdentification userId,
        string userName,
        int feeTotal,
        int feeToPay)
    {
        ClaimId = claimId;
        UserId = userId;
        UserName = userName;
        FeeTotal = feeTotal;
        FeeToPay = feeToPay;
    }
}
