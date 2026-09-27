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
    ICurrentUserAccessor currentUserAccessor)
    : IAccommodationInviteClient
{
    //TODO[Localize]
    private const string GroupSubtext = "(группа проживающих)";
    //TODO[Localize]
    private const string NoRequestSubtext = "еще не выбрал тип проживания";

    public async Task<AccommodationInviteTargetsViewModel> GetInviteTargets(ClaimIdentification claimId)
    {
        // Список соседей — не публичные данные, поэтому доступ такой же, как у самой панели проживания
        _ = (await claimsRepository.GetClaim(claimId))
            .RequestAccess(currentUserAccessor.UserId,
                Permission.CanSetPlayersAccommodations,
                ExtraAccessReason.PlayerOrResponsible);

        var acceptedRequest = (await accommodationRequestRepository.GetAccommodationRequestForClaim(claimId.ClaimId))
            .FirstOrDefault(request => request.IsAccepted == InviteState.Accepted);

        if (acceptedRequest is null)
        {
            return new AccommodationInviteTargetsViewModel(SenderRequestId: null, RoomFreeSpace: 0, Targets: []);
        }

        var senderRequestId = new AccommodationRequestIdentification(claimId.ProjectId, acceptedRequest.Id);
        var roomFreeSpace = acceptedRequest.GetRoomFreeSpace();

        var currentNeighbors = (await accommodationRequestRepository
                .GetClaimsWithSameAccommodationRequest(claimId.ProjectId, acceptedRequest.Id))
            .ToHashSet();

        var withSameType = (await accommodationRequestRepository
                .GetClaimsWithSameAccommodationTypeToInvite(claimId.ProjectId, acceptedRequest.AccommodationTypeId))
            .Where(c => c.ClaimId != claimId);
        var withoutRequest = await accommodationRequestRepository
            .GetClaimsWithOutAccommodationRequest(claimId.ProjectId);

        var potentialNeighbors = withSameType
            .Union(withoutRequest)
            .Where(c => !currentNeighbors.Contains(c.ClaimId))
            .ToArray();

        // Уже сложившиеся группы приглашаются целиком, поэтому те, кому не хватит места, отсеиваются
        var groupedTargets = potentialNeighbors
            .Where(c => c.AccommodationRequestId != null)
            .GroupBy(c => c.AccommodationRequestId!.Value)
            .Where(group => group.Count() <= roomFreeSpace)
            .Select(group => new AccommodationInviteTargetViewModel(
                AccommodationTargetIdentification.From(
                    new AccommodationRequestIdentification(claimId.ProjectId, group.Key)),
                Text: JoinNames(group, GetPlayerName, GetCharacterName),
                ExtraSearch: JoinNames(group, GetCharacterName, GetPlayerName),
                Subtext: group.Count() > 1 ? GroupSubtext : ""));

        var singleTargets = potentialNeighbors
            .Where(c => c.AccommodationRequestId == null)
            .Select(c => new AccommodationInviteTargetViewModel(
                AccommodationTargetIdentification.From(c.ClaimId),
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
        var model = await GetInviteTargets(claimId);
        if (model.SenderRequestId is null)
        {
            //TODO[Localize]
            throw new AccommodationInviteNotAllowedException(claimId.ProjectId,
                "Сначала надо выбрать тип проживания.");
        }

        await accommodationInviteService.CreateAccommodationInvite(claimId, model.SenderRequestId, target);
    }

    public async Task<IReadOnlyCollection<AccommodationInviteViewModel>> GetInvites(
        ClaimIdentification claimId,
        InviteDirection direction)
    {
        var claim = (await claimsRepository.GetClaim(claimId))
            .RequestAccess(currentUserAccessor.UserId,
                Permission.CanSetPlayersAccommodations,
                ExtraAccessReason.PlayerOrResponsible);

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
        var currentNeighbors = claim.AccommodationRequest is null
            ? []
            : (await accommodationRequestRepository
                    .GetClaimsWithSameAccommodationRequest(claimId.ProjectId, claim.AccommodationRequest.Id))
                .ToHashSet();

        return
        [
            .. visible
                .Where(invite => !currentNeighbors.Contains(Counterparty(invite, direction).GetId()))
                .Select(invite => new AccommodationInviteViewModel(
                    new AccommodationInviteIdentification(claimId.ProjectId, invite.Id),
                    ToUserLink(Counterparty(invite, direction).Player),
                    invite.IsAccepted)),
        ];
    }

    public async Task AcceptInvite(AccommodationInviteIdentification inviteId)
        => await accommodationInviteService.AcceptAccommodationInvite(inviteId);

    public async Task DeclineInvite(AccommodationInviteIdentification inviteId)
        => await accommodationInviteService.CancelOrDeclineAccommodationInvite(inviteId, InviteState.Declined);

    public async Task CancelInvite(AccommodationInviteIdentification inviteId)
        => await accommodationInviteService.CancelOrDeclineAccommodationInvite(inviteId, InviteState.Canceled);

    /// <summary>Вторая сторона приглашения: кто пригласил нас либо кого пригласили мы</summary>
    private static Claim Counterparty(AccommodationInvite invite, InviteDirection direction)
        => direction == InviteDirection.Incoming ? invite.From : invite.To;

    private static UserLinkViewModel ToUserLink(User user)
        => new(user.ToUserInfoHeader());

    private static string GetPlayerName(AccommodationNeighbourCandidate candidate) => candidate.Player.DisplayName;

    private static string GetCharacterName(AccommodationNeighbourCandidate candidate) => candidate.CharacterName;

    /// <summary>
    /// Склеивает имена участников группы через запятую. Если основное имя пустое, берётся запасное —
    /// так строка никогда не оказывается пустой в списке.
    /// </summary>
    private static string JoinNames(
        IEnumerable<AccommodationNeighbourCandidate> claims,
        Func<AccommodationNeighbourCandidate, string> primary,
        Func<AccommodationNeighbourCandidate, string> fallback)
        => string.Join(", ", claims.Select(claim =>
        {
            var name = primary(claim);
            return string.IsNullOrWhiteSpace(name) ? fallback(claim) : name;
        }));
}
