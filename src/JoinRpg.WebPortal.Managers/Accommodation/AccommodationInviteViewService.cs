using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Accommodation;

namespace JoinRpg.WebPortal.Managers.Accommodation;

/// <summary>
/// Серверная сторона контрола приглашения к совместному проживанию. Здесь же живёт группировка
/// потенциальных соседей — раньше она считалась прямо в Views/Claim/_ClaimAccommodation.cshtml.
/// </summary>
internal class AccommodationInviteViewService(
    IClaimsRepository claimsRepository,
    IClaimInfoRepository claimInfoRepository,
    IRoomCategoryPlanRepository roomCategoryPlanRepository,
    IAccommodationInviteRepository accommodationInviteRepository,
    IAccommodationInviteService accommodationInviteService,
    ICurrentUserAccessor currentUserAccessor)
    : IAccommodationInviteClient
{
    //TODO[Localize]
    private const string GroupSubtext = "(группа проживающих)";
    //TODO[Localize]
    private const string NoRequestSubtext = "еще не выбрал тип проживания";

    public async Task<AccommodationInviteTargetsViewModel> GetInviteTargets(ClaimIdentification claimId)
    {
        var (mode, sender) = await GetSenderGroupAndMode(claimId);
        if (sender is null || mode == AccommodationInviteMode.None)
        {
            // Без места контрол молчит — так и надо: тип не выбран либо сделать ничего нельзя.
            return new AccommodationInviteTargetsViewModel(SenderRequestId: null, RoomFreeSpace: 0, Targets: [], mode);
        }

        var (plan, senderGroup) = sender.Value;

        // Тот же расчёт, что и при приёме приглашения (AccommodationInviteServiceImpl): иначе
        // список предложил бы место, в котором операция откажет.
        var roomFreeSpace = plan.GetFreeSpaceForGroup(senderGroup.Id);

        // Уже сложившиеся группы приглашаются целиком: в кандидатах только нерасселённые группы
        // своего типа, на которые хватит места. Размер — полный состав, как его считает приём
        // приглашения, а не число утверждённых в группе (ADR022 §3).
        var candidateGroups = plan.UnassignedGroups
            .Where(group => group.AccommodationTypeId == senderGroup.AccommodationTypeId
                && group.Id != senderGroup.Id
                && group.SubjectsCount <= roomFreeSpace)
            .ToArray();

        // Фильтра по статусу нет: в группу проживающих попадает только утверждённая заявка, а при
        // отклонении она из группы выбывает (ClaimServiceImpl.ConsiderLeavingRoom).
        var headers = (await claimsRepository.GetClaimHeadersWithPlayer(
                [.. candidateGroups.SelectMany(group => group.Subjects)]))
            .ToDictionary(header => header.ClaimId);

        var groupedTargets = candidateGroups
            .Select(group => (Group: group, Members: group.Subjects
                .Where(headers.ContainsKey)
                .Select(id => headers[id])
                .ToArray()))
            .Where(candidate => candidate.Members.Length > 0)
            .Select(candidate => new AccommodationInviteTargetViewModel(
                AccommodationGroupIdentification.From(candidate.Group.Id),
                Text: string.Join(", ", candidate.Members.Select(GetPlayerName)),
                ExtraSearch: string.Join(", ", candidate.Members.Select(GetCharacterName)),
                Subtext: candidate.Group.SubjectsCount > 1 ? GroupSubtext : ""));

        // Одиночек без типа ни в одном плане нет — они приходят отдельным запросом.
        var singleTargets = (await claimsRepository.GetApprovedClaimHeadersWithoutAccommodation(claimId.ProjectId))
            .Select(header => new AccommodationInviteTargetViewModel(
                AccommodationGroupIdentification.From(header.ClaimId),
                Text: GetPlayerName(header),
                ExtraSearch: GetCharacterName(header),
                Subtext: NoRequestSubtext));

        return new AccommodationInviteTargetsViewModel(
            senderGroup.Id,
            roomFreeSpace,
            [.. groupedTargets, .. singleTargets],
            mode);
    }

    public async Task CreateInvite(ClaimIdentification claimId, AccommodationGroupIdentification target)
    {
        // Только группа приглашающего — список целей ради неё строить незачем: цель проверяет
        // сервис приглашений.
        var sender = (await GetSenderGroupAndMode(claimId)).Sender
            //TODO[Localize]
            ?? throw new AccommodationInviteNotAllowedException(claimId.ProjectId,
                "Сначала надо выбрать тип проживания.");

        await accommodationInviteService.CreateAccommodationInvite(claimId, sender.Group.Id, target);
    }

    // Все проверки — в сервисе: и права, и тип, и место.
    public async Task MergeGroups(ClaimIdentification claimId, AccommodationGroupIdentification target)
        => await accommodationInviteService.MergeAccommodationGroups(claimId, target);

    /// <summary>
    /// Что текущий пользователь может сделать с соседями по заявке, вместе с планом поселения и
    /// группой заявки (<c>null</c>, если тип проживания по заявке не выбран).
    /// </summary>
    /// <remarks>
    /// Доступ ровно как у операций с проживанием: кому они откажут, тому и список не отдаём (#5261).
    /// Режим решается по праву расселять, а не по тому, чья заявка: мастер с этим правом поселяет
    /// вместе даже на своей заявке; без права приглашает только сам игрок, а ответственному мастеру
    /// не остаётся ничего (#5656).
    /// </remarks>
    private async Task<(AccommodationInviteMode Mode, (RoomCategoryPlan Plan, AccommodationGroupInfo Group)? Sender)>
        GetSenderGroupAndMode(ClaimIdentification claimId)
    {
        var userId = currentUserAccessor.UserIdentificationOrDefault;
        var claimInCharacter = await claimInfoRepository.GetClaimInCharacterOrDefault(claimId)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, nameof(ClaimIdentification));
        var claimInfo = claimInCharacter
            .RequestAccommodationChangeAccess(userId)
            .Claim;

        var mode = claimInCharacter.Character.ProjectInfo.HasMasterAccess(userId, Permission.CanSetPlayersAccommodations)
            ? AccommodationInviteMode.Merge
            : claimInfo.PlayerId == userId ? AccommodationInviteMode.Invite : AccommodationInviteMode.None;

        if (claimInfo.AccommodationTypeId is not { } typeId)
        {
            return (mode, null);
        }

        var plan = await roomCategoryPlanRepository.GetPlanForTypeOrDefault(typeId)
            ?? throw new AccommodationTypeNotFoundException(typeId);

        // Тип задан — значит, ссылка указывает на группу (инвариант CharacterClaimInfo, ADR022).
        return (mode, (plan, plan.GetGroupOrDefault(claimInfo.AccommodationGroupId)!));
    }
    public async Task<IReadOnlyCollection<AccommodationInviteViewModel>> GetInvites(
        ClaimIdentification claimId,
        InviteDirection direction)
    {
        // Панель приглашений — это кнопки ответа на них, поэтому доступ как у ответа (#5261)
        var claim = (await claimsRepository.GetClaim(claimId))
            .RequestAccommodationChangeAccess(currentUserAccessor.UserIdentificationOrDefault);

        var invites = direction switch
        {
            InviteDirection.Incoming => await accommodationInviteRepository.GetIncomingInviteForClaim(claim),
            InviteDirection.Outgoing => await accommodationInviteRepository.GetOutgoingInviteForClaim(claim),
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };

        // Принятые приглашения не показываем — они уже превратились в соседство по комнате
        var visible = invites.Where(invite => invite.IsAccepted != InviteState.Accepted).ToArray();

        // Отвечать на приглашения может только сам игрок; мастер их только видит (#5656).
        var canRespond = claim.PlayerUserId == currentUserAccessor.UserIdOrDefault;
        if (visible.Length == 0)
        {
            return [];
        }

        // Приглашения от/к тем, кто уже в нашей группе, тоже не показываем. Сравниваем внешние
        // ключи — скалярные колонки заявок, без навигации на саму группу.
        bool IsSameGroup(Claim other)
            => claim.AccommodationRequest_Id is { } ownGroupId && other.AccommodationRequest_Id == ownGroupId;

        return
        [
            .. visible
                .Where(invite => !IsSameGroup(Counterparty(invite, direction)))
                .Select(invite => new AccommodationInviteViewModel(
                    new AccommodationInviteIdentification(claimId.ProjectId, invite.Id),
                    ToUserLink(Counterparty(invite, direction).Player),
                    invite.IsAccepted,
                    canRespond)),
        ];
    }

    public async Task AcceptInvite(AccommodationInviteIdentification inviteId)
        => await accommodationInviteService.AcceptAccommodationInvite(inviteId);

    public async Task DeclineInvite(AccommodationInviteIdentification inviteId)
        => await accommodationInviteService.DeclineAccommodationInvite(inviteId);

    public async Task CancelInvite(AccommodationInviteIdentification inviteId)
        => await accommodationInviteService.CancelAccommodationInvite(inviteId);

    /// <summary>Вторая сторона приглашения: кто пригласил нас либо кого пригласили мы</summary>
    private static Claim Counterparty(AccommodationInvite invite, InviteDirection direction)
        => direction == InviteDirection.Incoming ? invite.From : invite.To;

    private static UserLinkViewModel ToUserLink(User user)
        => new(user.ToUserInfoHeader());

    private static string GetPlayerName(ClaimWithPlayer claim) => claim.Player.DisplayName.DisplayName;

    private static string GetCharacterName(ClaimWithPlayer claim) => claim.CharacterName;
}
