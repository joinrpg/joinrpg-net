using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.Characters.Claims.Accommodation;

/// <summary>
/// Идентификатор заявки на проживание. Заявка на проживание принадлежит проекту, а не заявке игрока:
/// одну заявку на проживание делят все соседи по комнате.
/// </summary>
[method: JsonConstructor]
[TypedEntityId]
public partial record AccommodationRequestIdentification(
    ProjectIdentification ProjectId,
    int AccommodationRequestId) : IProjectEntityId;

/// <summary>
/// Идентификатор типа проживания в проекте (палатка, домик, номер в отеле…).
/// </summary>
[method: JsonConstructor]
[TypedEntityId]
public partial record AccommodationTypeIdentification(
    ProjectIdentification ProjectId,
    int AccommodationTypeId) : IProjectEntityId;

/// <summary>
/// Идентификатор комнаты (места поселения) в проекте.
/// </summary>
[method: JsonConstructor]
[TypedEntityId]
public partial record AccommodationRoomIdentification(
    ProjectIdentification ProjectId,
    int RoomId) : IProjectEntityId;

/// <summary>
/// Идентификатор категории комнат — пула, из которого селятся типы проживания.
/// </summary>
/// <remarks>
/// Получить категорию по типу проживания можно только через метаданные —
/// <c>AccommodationTypeInfo.RoomCategoryId</c>; конвертации идентификаторов в домене нет и заводить
/// её нельзя: из одной категории селятся несколько типов (ADR020).
/// </remarks>
[method: JsonConstructor]
[TypedEntityId]
public partial record RoomCategoryIdentification(
    ProjectIdentification ProjectId,
    int RoomCategoryId) : IProjectEntityId;

/// <summary>
/// Идентификатор приглашения к совместному проживанию.
/// </summary>
[method: JsonConstructor]
[TypedEntityId]
public partial record AccommodationInviteIdentification(
    ProjectIdentification ProjectId,
    int AccommodationInviteId) : IProjectEntityId;

/// <summary>
/// Группа проживающих, в которой состоит заявка: либо сложившаяся группа соседей (заявка на
/// проживание), либо сама заявка игрока как одиночная группа (он ещё не выбрал тип проживания).
/// </summary>
/// <remarks>
/// <para>
/// Используется как цель приглашения к совместному проживанию и как ссылка заявки на её группу
/// (ADR022).
/// </para>
/// <para>
/// Внутри — знаковое число: положительное значение это <see cref="ClaimIdentification"/>,
/// отрицательное — <see cref="AccommodationRequestIdentification"/>. Это единственное место в системе,
/// где знак интерпретируется; наружу тип отдаёт только типизированные идентификаторы.
/// </para>
/// <para>
/// Раньше тип назывался «целью приглашения»; строковая форма со старым префиксом
/// (<c>AccommodationTargetId(…)</c>) продолжает разбираться — она могла остаться в адресах виджета.
/// </para>
/// </remarks>
[method: JsonConstructor]
[TypedEntityId(AdditionalPrefixes = new[] { "AccommodationTargetId", "AccommodationTarget" })]
public partial record AccommodationGroupIdentification(
    ProjectIdentification ProjectId,
    int SignedValue) : IProjectEntityId
{
    /// <summary>Заявка игрока, если группа одиночная, иначе <c>null</c></summary>
    public ClaimIdentification? AsClaimId()
        => SignedValue > 0 ? new ClaimIdentification(ProjectId, SignedValue) : null;

    /// <summary>Заявка на проживание, если это сложившаяся группа, иначе <c>null</c></summary>
    public AccommodationRequestIdentification? AsAccommodationRequestId()
        => SignedValue < 0 ? new AccommodationRequestIdentification(ProjectId, -SignedValue) : null;

    /// <summary>Заявка игрока как одиночная группа</summary>
    public static AccommodationGroupIdentification From(ClaimIdentification claimId)
        => new(claimId.ProjectId, claimId.ClaimId);

    /// <summary>Сложившаяся группа проживающих</summary>
    public static AccommodationGroupIdentification From(AccommodationRequestIdentification requestId)
        => new(requestId.ProjectId, -requestId.AccommodationRequestId);
}
