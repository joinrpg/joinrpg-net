using JoinRpg.DataModel;
using JoinRpg.TestHelpers;
using Shouldly;
using Xunit;

namespace JoinRpg.Domain.Test
{
    public class ProjectFieldTypeTests
    {
        [Theory]
        [MemberData(nameof(FieldTypes))]
        public void PricingDecided(ProjectFieldType projectFieldType) => Should.NotThrow(() => projectFieldType.SupportsPricing());

        [Theory]
        [MemberData(nameof(FieldTypes))]
        public void PricingOnFieldDecided(ProjectFieldType projectFieldType) => Should.NotThrow(() => projectFieldType.SupportsPricingOnField());

        [Theory]
        [MemberData(nameof(FieldTypes))]
        public void HasValuesListDecided(ProjectFieldType projectFieldType) => Should.NotThrow(() => projectFieldType.HasValuesList());

        [Theory]
        [MemberData(nameof(FieldTypes))]
        public void ShouldBeAbleToCalculatePricing(ProjectFieldType projectFieldType)
        {
            var fieldWithValue = new FieldWithValue(new ProjectField { FieldType = projectFieldType }, null);
            Should.NotThrow(() => fieldWithValue.GetCurrentFee());
        }

        // ReSharper disable once MemberCanBePrivate.Global xUnit requirements
        public static TheoryData<ProjectFieldType> FieldTypes =>
            EnumerationTestHelper.GetTheoryDataForAllEnumValues<ProjectFieldType>();
    }
}
