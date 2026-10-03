using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectMasterTools.Print;

namespace JoinRpg.WebPortal.Managers.Print;

/// <summary>
/// Заглавная страница конверта — надпись, которую печатают и на конверте, и на наклейке (ADR013).
/// </summary>
/// <remarks>
/// Живёт здесь, а не во вью-модели печати: конверт собирается из трёх источников — агрегата
/// персонажа, телефонов игроков и планов поселения, — и каждый грузится один раз на всю пачку.
/// Печать открывается на весь проект, поэтому запрос на персонажа здесь недопустим.
/// </remarks>
public class PrintViewService(
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository,
    IRoomCategoryPlanRepository roomCategoryPlanRepository)
{
    /// <summary>Конверт одного персонажа — для вызывающих, у которых агрегат уже загружен.</summary>
    public async Task<EnvelopeViewModel> GetEnvelope(CharacterInfo character)
        => (await GetEnvelopes([character])).Single();

    /// <summary>
    /// Конверты персонажей, агрегаты которых вызывающий уже загрузил: страницы печати
    /// показывают рядом с конвертом поля и вводные, и агрегат им нужен всё равно.
    /// </summary>
    public async Task<IReadOnlyList<EnvelopeViewModel>> GetEnvelopes(
        IReadOnlyCollection<CharacterInfo> characters)
    {
        if (characters.Count == 0)
        {
            return [];
        }

        // Все агрегаты одного проекта разделяют экземпляр ProjectInfo (ADR013).
        var projectInfo = characters.First().ProjectInfo;

        var playerIds = characters
            .Select(character => character.ApprovedClaim?.PlayerId)
            .OfType<UserIdentification>()
            .Distinct()
            .ToList();

        var phones = await userRepository.GetPhoneNumbers(playerIds);

        // Планы поселения нужны только ради названия комнаты, а выборка это весь пул комнат проекта
        // с жильцами. Поэтому спрашиваем их, только если проживание выбрано хоть у кого-то в пачке:
        // печать одного конверта игрока без проживания не должна тянуть расселение всего проекта.
        // Заодно печать проекта, где проживания нет, не зависит от состояния его комнат.
        var needRooms = projectInfo.AccommodationSettings.Enabled
            && characters.Any(character => character.ApprovedClaim?.AccommodationTypeId is not null);

        var roomByClaim = needRooms
            ? (await roomCategoryPlanRepository.GetAllPlans(projectInfo.ProjectId)).BuildRoomByClaimIndex()
            : new Dictionary<ClaimIdentification, RoomInfo>();

        return [.. characters.Select(character => Build(character, phones, roomByClaim))];
    }

    /// <summary>
    /// Конверты активных персонажей из перечисленных — для печати наклеек пачкой.
    /// </summary>
    public async Task<IReadOnlyCollection<EnvelopeViewModel>> GetEnvelopesForActiveCharacters(
        IReadOnlyCollection<CharacterIdentification> characterIds)
    {
        var characters = await characterInfoRepository.GetCharacterInfos(characterIds);

        return await GetEnvelopes([.. characters.Where(character => character.IsActive)]);
    }

    /// <summary>Конверты всех активных персонажей проекта.</summary>
    public async Task<IReadOnlyCollection<EnvelopeViewModel>> GetEnvelopesForActiveCharacters(
        ProjectIdentification projectId)
    {
        var characters = await characterInfoRepository.GetAllCharacterInfos(projectId, CharacterStatusSpec.Active);

        return await GetEnvelopes(characters);
    }

    private static EnvelopeViewModel Build(
        CharacterInfo character,
        IReadOnlyDictionary<UserIdentification, PhoneNumber> phones,
        IReadOnlyDictionary<ClaimIdentification, RoomInfo> roomByClaim)
    {
        var projectInfo = character.ProjectInfo;
        var claim = character.ApprovedClaim;

        // Без утверждённой заявки взнос показывается по расписанию проекта на сегодня: конверт
        // печатают и на свободную роль, чтобы игрок на полигоне увидел цену.
        var feeDue = claim is null
            ? projectInfo.ProjectFinanceSettings.GetFeeForDate(DateTime.UtcNow, preferential: false)
            : character.CalculateClaimBalance(claim, projectInfo).FeeDue;

        var accommodationType = claim?.AccommodationTypeId is { } typeId
            ? projectInfo.AccommodationSettings.GetTypeByIdOrDefault(typeId)
            : null;

        return new EnvelopeViewModel(
            FeeDue: feeDue,
            CharacterId: character.Id,
            PlayerDisplayName: claim?.Player.DisplayName,
            CharacterName: character.CharacterName,
            ResponsibleMaster: new UserLinkViewModel(projectInfo.GetMasterById(character.ResponsibleMasterId).UserInfo),
            Groups: [.. character.IntrestingGroupsForDisplay.Where(group => group.IsPublic)
                .Select(group => new CharacterGroupLinkSlimViewModel(group))],
            PlayerPhoneNumber: claim is null ? null : phones.GetValueOrDefault(claim.PlayerId)?.Value,
            ProjectName: projectInfo.ProjectName,
            AccommodationEnabled: projectInfo.AccommodationSettings.Enabled,
            AccommodationTypeName: accommodationType?.Name,
            // Заявка с выбранным проживанием, но ещё не расселённая, комнаты не имеет —
            // компонент конверта покажет «Неизвестно».
            AccommodationName: claim is null ? null : roomByClaim.GetValueOrDefault(claim.ClaimId)?.Name);
    }
}
