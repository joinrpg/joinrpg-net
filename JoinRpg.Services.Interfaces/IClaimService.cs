using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IClaimService
  {
    Task AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, int currentUserId, string claimText);

    Task AddComment(int projectId, int claimId, int currentUserId, int? parentCommentId, bool isVisibleToPlayer,
      bool isMyClaim, string commentText);

    Task AppoveByMaster(int projectId, int claimId, int currentUserId, string commentText);
    Task DeclineByMaster(int projectId, int claimId, int currentUserId, string commentText);
    Task DeclineByPlayer(int projectId, int claimId, int currentUserId, string commentText);
    Task SetResponsible(int projectId, int claimId, int currentUserId, int responsibleMasterId);

    Task<IList<ClaimProblem>> GetProblemClaims(int projectId);
    Task RestoreByMaster(int projectId, int claimId, int currentUserId, string commentText);
    Task MoveByMaster (int projectId, int claimId, int currentUserId, string contents, int? characterGroupId, int? characterId);
  }

  public class ClaimProblem
  {
    public Claim Claim { get; }
    public ClaimProblemType ProblemType{ get; }

    public DateTime? ProblemTime { get; }

    public ClaimProblem(Claim claim, ClaimProblemType problemType, DateTime? problemTime = null)
    {
      Claim = claim;
      ProblemType = problemType;
      ProblemTime = problemTime;
    }
  }

  public enum ClaimProblemType 
  {
    NoResponsibleMaster,
    InvalidResponsibleMaster,
    ClaimNeverAnswered,
    ClaimNoDecision,
    ClaimActiveButCharacterHasApprovedClaim
  }
}