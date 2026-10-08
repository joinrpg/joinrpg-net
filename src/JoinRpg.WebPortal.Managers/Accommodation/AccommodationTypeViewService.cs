using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Interfaces.Characters;
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
    IClaimInfoRepository claimInfoRepository,
    IRoomCategoryPlanRepository roomCategoryPlanRepository,
    IClaimService claimService,
    ICurrentUserAccessor currentUserAccessor)
    : IAccommodationTypeClient
{
    public async Task<AccommodationTypeChoiceViewModel> GetAccommodationTypes(ClaimIdentification claimId)
    {
        // Тип и группа — из доменного снимка заявки, комната и соседи — из плана поселения (ADR022).
        var claimInCharacter = await claimInfoRepository.GetClaimInCharacterOrDefault(claimId)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, nameof(ClaimIdentification));
        var projectInfo = claimInCharacter.Character.ProjectInfo;

        // Варианты нужны только диалогу смены типа, поэтому доступ как у самой смены (#5261)
        var claimInfo = claimInCharacter
            .RequestAccommodationChangeAccess(currentUserAccessor.UserIdentificationOrDefault)
            .Claim;

        var hasMasterAccess = projectInfo.HasMasterAccess(currentUserAccessor);

        var selectedTypeId = claimInfo.AccommodationTypeId;
        var group = selectedTypeId is null
            ? null
            : (await roomCategoryPlanRepository.GetPlanForTypeOrDefault(selectedTypeId)
                ?? throw new AccommodationTypeNotFoundException(selectedTypeId))
                .GetGroupOrDefault(claimInfo.AccommodationGroupId);

        // Мастеру показываем всё, игроку — только помеченное как выбираемое, плюс то, что у него уже стоит.
        // Готовый ProjectAccommodationSettings.PlayerSelectableTypes здесь не подходит: к нему всё равно
        // пришлось бы приклеивать уже выбранный тип, и порядок типов поехал бы.
        var types = projectInfo.AccommodationSettings.Types
            .Where(type => type.IsPlayerSelectable
                || type.Id == selectedTypeId
                || hasMasterAccess)
            .Select(type => new AccommodationTypeViewModel(type, type.Description.ToHtmlString().Value))
            .ToArray();

        return new AccommodationTypeChoiceViewModel(
            types,
            selectedTypeId,
            RoomAssigned: group?.RoomId is not null,
            HasNeighbours: group?.SubjectsCount > 1);
    }

    public async Task SetAccommodationType(ClaimIdentification claimId, AccommodationTypeIdentification typeId)
    {
        _ = new IProjectEntityId[] { typeId }.EnsureProject(claimId.ProjectId);
        await claimService.SetAccommodationType(
            claimId.ProjectId.Value,
            claimId.ClaimId,
            typeId.AccommodationTypeId);
    }
}
