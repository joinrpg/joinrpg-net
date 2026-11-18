using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation;

namespace JoinRpg.Blazor.Client.ApiClients;

public class AccommodationInviteClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider)
    : IAccommodationInviteClient, IAccommodationTypeClient
{
    public async Task<AccommodationInviteTargetsViewModel> GetInviteTargets(ClaimIdentification claimId)
        => await (await httpClient.GetAsync($"webapi/AccommodationInvite/GetInviteTargets?claimId={claimId}"))
            .ReadFromJsonOrThrowProblem<AccommodationInviteTargetsViewModel>();

    public async Task CreateInvite(ClaimIdentification claimId, AccommodationGroupIdentification target)
        => await Post($"webapi/AccommodationInvite/CreateInvite?claimId={claimId}&target={target}");

    public async Task MergeGroups(ClaimIdentification claimId, AccommodationGroupIdentification target)
        => await Post($"webapi/AccommodationInvite/MergeGroups?claimId={claimId}&target={target}");

    public async Task<IReadOnlyCollection<AccommodationInviteViewModel>> GetInvites(
        ClaimIdentification claimId,
        InviteDirection direction)
        => await (await httpClient.GetAsync($"webapi/AccommodationInvite/GetInvites?claimId={claimId}&direction={direction}"))
            .ReadFromJsonOrThrowProblem<IReadOnlyCollection<AccommodationInviteViewModel>>();

    public async Task AcceptInvite(AccommodationInviteIdentification inviteId)
        => await Post($"webapi/AccommodationInvite/AcceptInvite?inviteId={inviteId}");

    public async Task DeclineInvite(AccommodationInviteIdentification inviteId)
        => await Post($"webapi/AccommodationInvite/DeclineInvite?inviteId={inviteId}");

    public async Task CancelInvite(AccommodationInviteIdentification inviteId)
        => await Post($"webapi/AccommodationInvite/CancelInvite?inviteId={inviteId}");

    public async Task<AccommodationTypeChoiceViewModel> GetAccommodationTypes(ClaimIdentification claimId)
        => await (await httpClient.GetAsync($"webapi/AccommodationInvite/GetAccommodationTypes?claimId={claimId}"))
            .ReadFromJsonOrThrowProblem<AccommodationTypeChoiceViewModel>();

    public async Task SetAccommodationType(ClaimIdentification claimId, AccommodationTypeIdentification typeId)
        => await Post($"webapi/AccommodationInvite/SetAccommodationType?claimId={claimId}&typeId={typeId}");

    private async Task Post(string uri)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(uri, content: null);

        // Причину отказа контрол показывает игроку
        await response.EnsureSuccessOrThrowProblem();
    }
}
