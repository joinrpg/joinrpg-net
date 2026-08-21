using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Accommodation;

namespace JoinRpg.WebPortal.Managers.Accommodation;

/// <summary>
/// Серверная сторона диалога выбора типа проживания. Фильтрация доступных вариантов раньше
/// жила в ClaimAccommodationViewModel, а разметка — в Views/Claim/_ClaimAccommodationTypeChange.cshtml.
/// </summary>
internal class AccommodationTypeViewService(
    IClaimsRepository claimsRepository,
    IAccommodationRepository accommodationRepository,
    IClaimService claimService,
    ICurrentUserAccessor currentUserAccessor)
    : IAccommodationTypeClient
{
    public async Task<AccommodationTypeChoiceViewModel> GetAccommodationTypes(ClaimIdentification claimId)
    {
        var claim = (await claimsRepository.GetClaim(claimId))
            .RequestAccess(currentUserAccessor.UserId,
                Permission.CanSetPlayersAccommodations,
                ExtraAccessReason.PlayerOrResponsible);

        var request = claim.AccommodationRequest;
        var hasMasterAccess = claim.HasMasterAccess(currentUserAccessor);

        // Мастеру показываем всё, игроку — только помеченное как выбираемое, плюс то, что у него уже стоит
        var types = (await accommodationRepository.GetAccommodationForProject(claimId.ProjectId.Value))
            .Where(type => type.IsPlayerSelectable
                || type.Id == request?.AccommodationTypeId
                || hasMasterAccess)
            .Select(type => new AccommodationTypeViewModel(
                new AccommodationTypeIdentification(claimId.ProjectId, type.Id),
                type.Name,
                type.Capacity,
                type.Cost,
                ((MarkdownString?)type.Description).ToHtmlString().Value))
            .ToArray();

        return new AccommodationTypeChoiceViewModel(
            types,
            request is null ? null : new AccommodationTypeIdentification(claimId.ProjectId, request.AccommodationTypeId),
            RoomAssigned: request?.Accommodation != null,
            HasNeighbours: request?.Subjects.Count > 1);
    }

    public async Task SetAccommodationType(ClaimIdentification claimId, AccommodationTypeIdentification typeId)
    {
        _ = new IProjectEntityId[] { typeId }.EnsureProject(claimId.ProjectId);
        _ = await claimService.SetAccommodationType(
            claimId.ProjectId.Value,
            claimId.ClaimId,
            typeId.AccommodationTypeId);
    }
}
