namespace JoinRpg.Web.Claims;

public interface IClaimOperationClient
{
    Task AllowSensitiveData(ProjectIdentification projectId);

    Task AcceptInvitation(ClaimIdentification claimId, string commentText);
}
