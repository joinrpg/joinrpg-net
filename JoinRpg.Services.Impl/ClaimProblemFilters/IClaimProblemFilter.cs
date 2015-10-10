using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  interface IClaimProblemFilter
  {
    IEnumerable<ClaimProblem> GetProblems(Project project, Claim claim);
  }


  internal static class ClaimProblemExts
  {
    public static ClaimProblem Problem(this Claim claim, ClaimProblemType type, DateTime? problemTime = null)
    {
      return new ClaimProblem(claim, type, problemTime);
    }
  }
}
