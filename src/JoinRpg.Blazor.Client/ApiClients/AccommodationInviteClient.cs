using System.Net;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation;

namespace JoinRpg.Blazor.Client.ApiClients;

public class AccommodationInviteClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider)
    : IAccommodationInviteClient, IAccommodationTypeClient
{
    public async Task<AccommodationInviteTargetsViewModel> GetInviteTargets(ClaimIdentification claimId)
        => await httpClient.GetFromJsonAsync<AccommodationInviteTargetsViewModel>(
            $"webapi/AccommodationInvite/GetInviteTargets?claimId={claimId}")
            ?? throw new Exception("Couldn't get result from server");

    public async Task CreateInvite(ClaimIdentification claimId, AccommodationTargetIdentification target)
        => await Post($"webapi/AccommodationInvite/CreateInvite?claimId={claimId}&target={target}");

    public async Task<IReadOnlyCollection<AccommodationInviteViewModel>> GetInvites(
        ClaimIdentification claimId,
        InviteDirection direction)
        => await httpClient.GetFromJsonAsync<IReadOnlyCollection<AccommodationInviteViewModel>>(
            $"webapi/AccommodationInvite/GetInvites?claimId={claimId}&direction={direction}")
            ?? throw new Exception("Couldn't get result from server");

    public async Task AcceptInvite(AccommodationInviteIdentification inviteId)
        => await Post($"webapi/AccommodationInvite/AcceptInvite?inviteId={inviteId}");

    public async Task DeclineInvite(AccommodationInviteIdentification inviteId)
        => await Post($"webapi/AccommodationInvite/DeclineInvite?inviteId={inviteId}");

    public async Task CancelInvite(AccommodationInviteIdentification inviteId)
        => await Post($"webapi/AccommodationInvite/CancelInvite?inviteId={inviteId}");

    public async Task<AccommodationTypeChoiceViewModel> GetAccommodationTypes(ClaimIdentification claimId)
        => await httpClient.GetFromJsonAsync<AccommodationTypeChoiceViewModel>(
            $"webapi/AccommodationInvite/GetAccommodationTypes?claimId={claimId}")
            ?? throw new Exception("Couldn't get result from server");

    public async Task SetAccommodationType(ClaimIdentification claimId, AccommodationTypeIdentification typeId)
        => await Post($"webapi/AccommodationInvite/SetAccommodationType?claimId={claimId}&typeId={typeId}");

    private async Task Post(string uri)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(uri, content: null);

        // Причина отказа приходит текстом, чтобы контрол мог показать её игроку
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        }

        response.EnsureSuccessStatusCode();
    }
}
