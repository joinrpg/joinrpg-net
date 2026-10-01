using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Accommodation;

namespace JoinRpg.WebPortal.Managers.Accommodation;

/// <summary>
/// Страница «Комнаты» одного типа проживания.
/// </summary>
/// <remarks>
/// Денег в плане поселения нет (ADR018, §5): жильцов он несёт идентификаторами заявок, а баланс
/// и имена игроков берутся из агрегата персонажа (ADR013) — одной общей выборкой на страницу,
/// а не по запросу на жильца.
/// </remarks>
public class RoomTypeRoomsViewService(
    IRoomCategoryPlanRepository roomCategoryPlanRepository,
    ICharacterInfoRepository characterInfoRepository,
    ICurrentUserAccessor currentUserAccessor)
{
    /// <summary>
    /// Модель страницы или <c>null</c>, если такого типа проживания в проекте нет.
    /// </summary>
    public async Task<RoomTypeViewModel?> GetRoomTypeRooms(AccommodationTypeIdentification typeId)
    {
        var plan = await roomCategoryPlanRepository.GetPlanForTypeOrDefault(typeId);
        if (plan is null)
        {
            return null;
        }

        return new RoomTypeViewModel(
            plan,
            typeId,
            // Markdown рендерится здесь, на сервере: вью-модель лежит в браузерной библиотеке
            // JoinRpg.Web.Accommodation и рендерера markdown не видит.
            plan.GetAccommodationType(typeId).Description.ToHtmlString(),
            await LoadParticipants(plan),
            currentUserAccessor.UserIdentification);
    }

    private async Task<IReadOnlyDictionary<ClaimIdentification, RequestParticipantViewModel>> LoadParticipants(
        RoomCategoryPlan plan)
    {
        var claimIds = plan.Groups.SelectMany(group => group.Subjects).ToHashSet();

        if (claimIds.Count == 0)
        {
            return new Dictionary<ClaimIdentification, RequestParticipantViewModel>();
        }

        var characters = await characterInfoRepository.GetCharacterInfosByClaims(claimIds);

        return characters
            .SelectMany(character => character.Claims.Select(claim => (character, claim)))
            .Where(x => claimIds.Contains(x.claim.ClaimId))
            .ToDictionary(
                x => x.claim.ClaimId,
                x => new RequestParticipantViewModel(x.character, x.claim, plan.ProjectInfo));
    }
}
