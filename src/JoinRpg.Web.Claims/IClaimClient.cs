namespace JoinRpg.Web.Claims;

public record AcceptInvitationRequest(string CommentText, bool SensitiveDataAllowed);

public interface IClaimOperationClient
{
    Task AllowSensitiveData(ProjectIdentification projectId);

    Task AcceptInvitation(ClaimIdentification claimId, AcceptInvitationRequest request);
}
