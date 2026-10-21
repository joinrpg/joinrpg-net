using JoinRpg.DomainTypes;

namespace JoinRpg.Web.Claims.Test;

internal sealed class FakeClaimOperationClient : IClaimOperationClient
{
    public Exception? Failure { get; set; }

    public Task AllowSensitiveData(ProjectIdentification projectId) => Result();

    public Task AcceptInvitation(ClaimIdentification claimId, AcceptInvitationRequest request) => Result();

    private Task Result() => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
}
