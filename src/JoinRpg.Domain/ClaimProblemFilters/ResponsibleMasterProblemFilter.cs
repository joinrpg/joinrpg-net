using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
    internal class ResponsibleMasterProblemFilter : IProblemFilter<Claim>
    {
        public IEnumerable<ClaimProblem> GetProblems(Claim claim)
        {
            if (claim.ResponsibleMasterUser == null)
            {
                yield return new ClaimProblem(ClaimProblemType.NoResponsibleMaster, ProblemSeverity.Error);
            }
            else if (!claim.HasMasterAccess(claim.ResponsibleMasterUserId))
            {
                yield return new ClaimProblem(ClaimProblemType.InvalidResponsibleMaster, ProblemSeverity.Error);
            }
        }
    }
}
