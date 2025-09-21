using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.Claims;

namespace JoinRpg.WebPortal.Models.Test;

public class ProblemTypeDisplayTest
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<ClaimProblemType>))]
    public void EveryProblemTypeShouldHaveVisualization(ClaimProblemType problemType)
    {
        var problem = new ClaimProblem(problemType, ProblemSeverity.Error);
        if (problemType.GetAttribute<ObsoleteAttribute>() is null)
        {
            _ = Should.NotThrow(() => new ProblemViewModel(problem));
        }
        else
        {
            _ = Should.Throw<KeyNotFoundException>(() => new ProblemViewModel(problem));
        }
    }
}
