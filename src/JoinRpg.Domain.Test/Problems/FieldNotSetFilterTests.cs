using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Domain.Problems.CommonProblemFilters;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test.Problems;

public class FieldNotSetFilterTests
{
    private readonly MockedProject mock = new();

    [Theory]
    [InlineData(MandatoryStatus.Optional, null)]
    [InlineData(MandatoryStatus.Recommended, ProblemSeverity.Warning)]
    [InlineData(MandatoryStatus.Required, ProblemSeverity.Warning)]
    public void RecommendedField_WhenEmpty_ProducesWarning(MandatoryStatus status, ProblemSeverity? expectedSeverity)
    {
        var field = mock.CreateField("Test field", canPlayerEdit: true, showOnUnApprovedClaims: true, mandatoryStatus: status);
        var fieldWithValue = new FieldWithValue(field, value: null);
        var target = new CharacterItem(mock.Character, []);

        var problems = ((IFieldRelatedProblemFilter<Claim>)new FieldNotSetFilter()).CheckField(target, fieldWithValue).ToList();

        if (expectedSeverity is null)
        {
            problems.ShouldBeEmpty();
        }
        else
        {
            problems.ShouldContain(p => p.ProblemType == ClaimProblemType.FieldIsEmpty && p.Severity == expectedSeverity);
        }
    }
}
