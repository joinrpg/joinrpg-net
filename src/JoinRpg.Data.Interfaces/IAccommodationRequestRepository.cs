using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Кандидат в соседи по номеру — ровно то, что нужно строке списка «кого пригласить».
/// </summary>
/// <param name="ClaimId">Заявка кандидата</param>
/// <param name="AccommodationRequestId">Заявка на проживание кандидата. <c>null</c> — тип проживания не выбран</param>
/// <param name="Player">Игрок по этой заявке</param>
/// <param name="CharacterName">Имя персонажа: показывается, если у игрока не заполнено имя, и участвует в поиске по списку</param>
/// <remarks>
/// Сущности тут отдавать нельзя: список строится по всем утверждённым заявкам проекта, и догрузка
/// персонажа по одному давала N+1 на каждое открытие страницы заявки (#4964).
/// </remarks>
public record AccommodationNeighbourCandidate(
    ClaimIdentification ClaimId,
    int? AccommodationRequestId,
    UserDisplayName Player,
    string CharacterName);

public interface IAccommodationRequestRepository
{
    Task<IReadOnlyCollection<AccommodationRequest>> GetAccommodationRequestForProject(int projectId);
    Task<IReadOnlyCollection<AccommodationRequest>> GetAccommodationRequestForClaim(int claimId);
    Task<IReadOnlyCollection<Claim>> GetClaimsWithSameAccommodationType(int accommodationTypeId);

    /// <summary>Утверждённые заявки, выбравшие этот тип проживания и ещё не поселённые в номер</summary>
    Task<IReadOnlyCollection<AccommodationNeighbourCandidate>> GetClaimsWithSameAccommodationTypeToInvite(
        ProjectIdentification projectId,
        int accommodationTypeId);

    /// <summary>Утверждённые заявки, живущие по этой заявке на проживание</summary>
    Task<IReadOnlyCollection<ClaimIdentification>> GetClaimsWithSameAccommodationRequest(
        ProjectIdentification projectId,
        int accommodationRequestId);

    /// <summary>Утверждённые заявки проекта, ещё не выбравшие тип проживания</summary>
    Task<IReadOnlyCollection<AccommodationNeighbourCandidate>> GetClaimsWithOutAccommodationRequest(
        ProjectIdentification projectId);
}
