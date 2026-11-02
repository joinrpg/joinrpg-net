using JoinRpg.DomainTypes;

namespace JoinRpg.Web.Claims.Test;

internal sealed class FakeClaimOperationClient : IClaimOperationClient
{
    public Exception? Failure { get; set; }

    /// <summary>Какую заявку просили принять автоматически кнопкой.</summary>
    public ClaimIdentification? AutoApproveRequestedClaim { get; private set; }

    public Task AllowSensitiveData(ProjectIdentification projectId) => Result();

    public Task AcceptInvitation(ClaimIdentification claimId, AcceptInvitationRequest request) => Result();

    public Task AutoApproveMyClaim(ClaimIdentification claimId)
    {
        AutoApproveRequestedClaim = claimId;
        return Result();
    }

    private Task Result() => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
}
