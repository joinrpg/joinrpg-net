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
/// Идентификатор приглашения к совместному проживанию.
/// </summary>
[method: JsonConstructor]
[TypedEntityId]
public partial record AccommodationInviteIdentification(
    ProjectIdentification ProjectId,
    int AccommodationInviteId) : IProjectEntityId;

/// <summary>
/// Цель приглашения к совместному проживанию: либо отдельная заявка игрока (он ещё не выбрал тип
/// проживания), либо целая заявка на проживание (сложившаяся группа соседей).
/// </summary>
/// <remarks>
/// Внутри — знаковое число: положительное значение это <see cref="ClaimIdentification"/>,
/// отрицательное — <see cref="AccommodationRequestIdentification"/>. Это единственное место в системе,
/// где знак интерпретируется; наружу тип отдаёт только типизированные идентификаторы.
/// </remarks>
[method: JsonConstructor]
[TypedEntityId]
public partial record AccommodationTargetIdentification(
    ProjectIdentification ProjectId,
    int SignedValue) : IProjectEntityId
{
    /// <summary>Заявка игрока, если приглашение адресовано ей, иначе <c>null</c></summary>
    public ClaimIdentification? AsClaimId()
        => SignedValue > 0 ? new ClaimIdentification(ProjectId, SignedValue) : null;

    /// <summary>Заявка на проживание, если приглашение адресовано ей, иначе <c>null</c></summary>
    public AccommodationRequestIdentification? AsAccommodationRequestId()
        => SignedValue < 0 ? new AccommodationRequestIdentification(ProjectId, -SignedValue) : null;

    /// <summary>Пригласить отдельную заявку игрока</summary>
    public static AccommodationTargetIdentification From(ClaimIdentification claimId)
        => new(claimId.ProjectId, claimId.ClaimId);

    /// <summary>Пригласить всю группу проживающих</summary>
    public static AccommodationTargetIdentification From(AccommodationRequestIdentification requestId)
        => new(requestId.ProjectId, -requestId.AccommodationRequestId);
}
