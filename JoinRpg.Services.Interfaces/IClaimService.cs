namespace JoinRpg.Services.Interfaces
{
  public interface IClaimService
  {
    void AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, int currentUserId, string claimText);
  }
}