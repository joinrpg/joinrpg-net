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
        public void PricingDecided(ProjectFieldType projectFieldType)
        {
            Should.NotThrow(() => projectFieldType.SupportsPricing());
        }

        [Theory]
        [MemberData(nameof(FieldTypes))]
        public void PricingOnFieldDecided(ProjectFieldType projectFieldType)
        {
            Should.NotThrow(() => projectFieldType.SupportsPricingOnField());
        }

        [Theory]
        [MemberData(nameof(FieldTypes))]
        public void HasValuesListDecided(ProjectFieldType projectFieldType)
        {
            Should.NotThrow(() => projectFieldType.HasValuesList());
        }

        // ReSharper disable once MemberCanBePrivate.Global xUnit requirements
        public static TheoryData<ProjectFieldType> FieldTypes =>
            EnumerationTestHelper.GetTheoryDataForAllEnumValues<ProjectFieldType>();
    }
}
