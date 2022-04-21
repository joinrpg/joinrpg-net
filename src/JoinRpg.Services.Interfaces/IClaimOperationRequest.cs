namespace JoinRpg.Services.Interfaces;

public interface IClaimOperationRequest
{
    int ProjectId { get; set; }
    int ClaimId { get; set; }
}
