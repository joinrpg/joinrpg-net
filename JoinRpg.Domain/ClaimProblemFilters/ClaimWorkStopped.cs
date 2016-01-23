using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
  internal class ClaimWorkStopped : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
      if (!claim.IsApproved) // Our concern is only approved claims
      {
        yield break;
      }

      if (DateTime.UtcNow.Subtract(claim.CreateDate) < TimeSpan.FromDays(2)) //If filed only recently, do nothing
      {
        yield break;
      }

      if (!claim.GetMasterAnswers().Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimNeverAnswered);
      }

      else if (!claim.GetMasterAnswers().InLastXDays(30).Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimWorkStopped, claim.GetMasterAnswers().Last().CreatedTime);
      }
    }
  }
}
