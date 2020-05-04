using System.Collections.Generic;

namespace JoinRpg.Domain.ClaimProblemFilters
{
    internal interface IProblemFilter<in TObject>
    {
        IEnumerable<ClaimProblem> GetProblems(TObject claim);
    }
}
