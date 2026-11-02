namespace JoinRpg.Web.Claims;

public record AcceptInvitationRequest(string CommentText, bool SensitiveDataAllowed);

public interface IClaimOperationClient
{
    Task AllowSensitiveData(ProjectIdentification projectId);

    Task AcceptInvitation(ClaimIdentification claimId, AcceptInvitationRequest request);

    /// <summary>Кнопка «Автопринять мою заявку»: принять заявку сейчас, когда блокеры сняты.</summary>
    Task AutoApproveMyClaim(ClaimIdentification claimId);
}
