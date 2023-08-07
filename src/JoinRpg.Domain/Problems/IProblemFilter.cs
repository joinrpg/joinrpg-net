using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems;

public interface IProblemFilter<in TObject> where TObject : IFieldContainter
{
    IEnumerable<ClaimProblem> GetProblems(TObject claim, ProjectInfo projectInfo);
}
