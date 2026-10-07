using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
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
    IAccommodationRequestRepository accommodationRequestRepository,
    IAccommodationInviteRepository accommodationInviteRepository,
    IAccommodationInviteService accommodationInviteService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor)
    : IAccommodationInviteClient
{
    //TODO[Localize]
    private const string GroupSubtext = "(группа проживающих)";
    //TODO[Localize]
    private const string NoRequestSubtext = "еще не выбрал тип проживания";

    public async Task<AccommodationInviteTargetsViewModel> GetInviteTargets(ClaimIdentification claimId)
    {
        var (claim, acceptedRequest) = await GetSenderRequest(claimId);

        if (acceptedRequest is null)
        {
            return new AccommodationInviteTargetsViewModel(SenderRequestId: null, RoomFreeSpace: 0, Targets: []);
        }

        var senderRequestId = new AccommodationRequestIdentification(claimId.ProjectId, acceptedRequest.Id);
        // Тот же расчёт, что и при приёме приглашения (AccommodationInviteServiceImpl): иначе
        // список предложил бы место, в котором операция откажет.
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);
        var roomFreeSpace = acceptedRequest.GetRoomFreeSpace(projectInfo);

        var currentNeighbors = (await accommodationRequestRepository
                .GetClaimsWithSameAccommodationRequest(acceptedRequest.Id))
            .Select(c => c.ClaimId)
            .ToHashSet();

        var withSameType = (await accommodationRequestRepository
                .GetClaimsWithSameAccommodationTypeToInvite(acceptedRequest.AccommodationTypeId))
            .Where(c => c.ClaimId != claim.ClaimId);
        var withoutRequest = await accommodationRequestRepository
            .GetClaimsWithOutAccommodationRequest(claimId.ProjectId.Value);

        var potentialNeighbors = withSameType
            .Union(withoutRequest)
            .Where(c => !currentNeighbors.Contains(c.ClaimId))
            .ToArray();

        // Уже сложившиеся группы приглашаются целиком, поэтому те, кому не хватит места, отсеиваются
        var groupedTargets = potentialNeighbors
            .Where(c => c.AccommodationRequest_Id != null)
            .GroupBy(c => c.AccommodationRequest_Id!.Value)
            .Where(group => group.Count() <= roomFreeSpace)
            .Select(group => new AccommodationInviteTargetViewModel(
                AccommodationTargetIdentification.From(
                    new AccommodationRequestIdentification(claimId.ProjectId, group.Key)),
                Text: JoinNames(group, GetPlayerName, GetCharacterName),
                ExtraSearch: JoinNames(group, GetCharacterName, GetPlayerName),
                Subtext: group.Count() > 1 ? GroupSubtext : ""));

        var singleTargets = potentialNeighbors
            .Where(c => c.AccommodationRequest_Id == null)
            .Select(c => new AccommodationInviteTargetViewModel(
                AccommodationTargetIdentification.From(c.GetId()),
                Text: GetPlayerName(c),
                ExtraSearch: GetCharacterName(c),
                Subtext: NoRequestSubtext));

        return new AccommodationInviteTargetsViewModel(
            senderRequestId,
            roomFreeSpace,
            [.. groupedTargets, .. singleTargets]);
    }

    public async Task CreateInvite(ClaimIdentification claimId, AccommodationTargetIdentification target)
    {
        // Весь список целей тут не нужен — только группа приглашающего. Раньше он строился целиком
        // и тащил за собой все свои загрузки (#4964)
        var (_, acceptedRequest) = await GetSenderRequest(claimId);
        if (acceptedRequest is null)
        {
            //TODO[Localize]
            throw new AccommodationInviteNotAllowedException(claimId.ProjectId,
                "Сначала надо выбрать тип проживания.");
        }

        await accommodationInviteService.CreateAccommodationInvite(
            claimId,
            new AccommodationRequestIdentification(claimId.ProjectId, acceptedRequest.Id),
            target);
    }

    /// <summary>
    /// Заявка приглашающего (с проверкой доступа) и её принятая группа проживающих, если она есть.
    /// </summary>
    private async Task<(Claim Claim, AccommodationRequest? AcceptedRequest)> GetSenderRequest(
        ClaimIdentification claimId)
    {
        // Список нужен только чтобы пригласить, поэтому доступ ровно как у самого приглашения:
        // кому операция откажет, тому и список не отдаём (#5261)
        var claim = (await claimsRepository.GetClaim(claimId))
            .RequestAccommodationChangeAccess(currentUserAccessor.UserIdentificationOrDefault);

        var acceptedRequest = (await accommodationRequestRepository.GetAccommodationRequestForClaim(claimId.ClaimId))
            .FirstOrDefault(request => request.IsAccepted == InviteState.Accepted);

        return (claim, acceptedRequest);
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
        if (visible.Length == 0)
        {
            return [];
        }

        // Приглашения от/к тем, с кем уже живём в одной комнате, тоже не показываем
        // Группа — по внешнему ключу: навигация AccommodationRequest не загружена (#4964)
        var currentNeighbors = claim.AccommodationRequest_Id is not int accommodationRequestId
            ? []
            : (await accommodationRequestRepository
                    .GetClaimsWithSameAccommodationRequest(accommodationRequestId))
                .Select(c => c.ClaimId)
                .ToHashSet();

        return
        [
            .. visible
                .Where(invite => !currentNeighbors.Contains(Counterparty(invite, direction).ClaimId))
                .Select(invite => new AccommodationInviteViewModel(
                    new AccommodationInviteIdentification(claimId.ProjectId, invite.Id),
                    ToUserLink(Counterparty(invite, direction).Player),
                    invite.IsAccepted)),
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

    private static string GetPlayerName(Claim claim) => claim.Player.GetDisplayName();

    private static string GetCharacterName(Claim claim) => claim.Character.CharacterName;

    /// <summary>
    /// Склеивает имена участников группы через запятую. Если основное имя пустое, берётся запасное —
    /// так строка никогда не оказывается пустой в списке.
    /// </summary>
    private static string JoinNames(
        IEnumerable<Claim> claims,
        Func<Claim, string> primary,
        Func<Claim, string> fallback)
        => string.Join(", ", claims.Select(claim =>
        {
            var name = primary(claim);
            return string.IsNullOrWhiteSpace(name) ? fallback(claim) : name;
        }));
}
