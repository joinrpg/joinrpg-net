namespace JoinRpg.Services.Interfaces;

public interface IClaimOperationRequest
{
    int ProjectId { get; }
    int ClaimId { get; set; }

    ProjectIdentification ProjectIdentification => new(ProjectId);
}
