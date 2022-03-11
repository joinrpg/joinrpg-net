using JoinRpg.DataModel;
using JoinRpg.TestHelpers;
using Shouldly;
using Xunit;

namespace JoinRpg.Domain.Test
{
    public class ProjectFieldTypeTests
    {
        [Theory]
        [ClassData(typeof(EnumTheoryDataGenerator<ProjectFieldType>))]
        public void PricingDecided(ProjectFieldType projectFieldType) => Should.NotThrow(() => projectFieldType.SupportsPricing());

        [Theory]
        [ClassData(typeof(EnumTheoryDataGenerator<ProjectFieldType>))]
        public void PricingOnFieldDecided(ProjectFieldType projectFieldType) => Should.NotThrow(() => projectFieldType.SupportsPricingOnField());

        [Theory]
        [ClassData(typeof(EnumTheoryDataGenerator<ProjectFieldType>))]
        public void HasValuesListDecided(ProjectFieldType projectFieldType) => Should.NotThrow(() => projectFieldType.HasValuesList());

        [Theory]
        [ClassData(typeof(EnumTheoryDataGenerator<ProjectFieldType>))]
        public void ShouldBeAbleToCalculatePricing(ProjectFieldType projectFieldType)
        {
            var fieldWithValue = new FieldWithValue(new ProjectField { FieldType = projectFieldType }, null);
            _ = Should.NotThrow(() => fieldWithValue.GetCurrentFee());
        }
    }
}
