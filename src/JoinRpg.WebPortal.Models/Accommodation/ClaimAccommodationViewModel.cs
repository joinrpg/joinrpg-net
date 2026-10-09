using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;

namespace JoinRpg.Web.Models.Accommodation;

/// <summary>
/// Панель «Проживание» на странице заявки. Приглашения и выбор типа проживания живут
/// в островах <c>JoinRpg.Web.Accommodation</c> и данных отсюда не берут.
/// </summary>
/// <remarks>
/// Строится из доменных снимков (ADR022): тип — из метаданных проекта по
/// <see cref="CharacterClaimInfo.AccommodationTypeId"/>, комната, свободные места и соседи — из
/// плана поселения. EF-группы проживающих панель больше не видит.
/// </remarks>
public class ClaimAccommodationViewModel
{
    /// <param name="claim">Заявка, для которой строится панель.</param>
    /// <param name="projectInfo">Метаданные проекта — источник типа проживания.</param>
    /// <param name="plan">
    /// План поселения типа заявки; <c>null</c>, если тип проживания не выбран.
    /// </param>
    /// <param name="neighbours">
    /// Соседи заявки по плану (<see cref="RoomCategoryPlan.GetNeighbours"/>), уже с именами игроков.
    /// </param>
    public ClaimAccommodationViewModel(
        CharacterClaimInfo claim,
        ProjectInfo projectInfo,
        RoomCategoryPlan? plan,
        IReadOnlyCollection<UserInfoHeader> neighbours)
    {
        ClaimIdentification = claim.ClaimId;
        Neighbours = [.. neighbours.Select(user => new UserLinkViewModel(user))];

        if (claim.AccommodationTypeId is { } typeId)
        {
            ArgumentNullException.ThrowIfNull(plan);

            AccommodationType = projectInfo.AccommodationSettings.GetTypeById(typeId);

            // Тип задан — значит, заявка в группе (инвариант CharacterClaimInfo, ADR022).
            var group = plan.GetGroupOrDefault(claim.AccommodationGroupId)!;
            RoomName = group.RoomId is { } roomId ? plan.GetRoom(roomId).Name : null;
            RoomFreeSpace = plan.GetFreeSpaceForGroup(group.Id);
        }
    }

    public ClaimIdentification ClaimIdentification { get; }

    public int ClaimId => ClaimIdentification.ClaimId;

    public int ProjectId => ClaimIdentification.ProjectId.Value;

    /// <summary>Выбранный тип проживания, или <c>null</c>, если проживание не выбрано.</summary>
    public AccommodationTypeInfo? AccommodationType { get; }

    /// <summary>Комната, в которой живёт группа заявки, или <c>null</c>, если ещё не расселена.</summary>
    public string? RoomName { get; }

    public int RoomFreeSpace { get; }

    public IReadOnlyCollection<UserLinkViewModel> Neighbours { get; }
}
