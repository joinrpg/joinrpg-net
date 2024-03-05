using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.TestHelpers;
using Shouldly;
using Xunit;

namespace JoinRpg.Domain.Test;

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
        var field = new ProjectFieldInfo(new PrimitiveTypes.ProjectFieldIdentification(new PrimitiveTypes.ProjectIdentification(-1), -1), "", projectFieldType, FieldBoundTo.Character, new ProjectFieldVariant[] { }, null, 1, true, true, true, true, MandatoryStatus.Optional, false, true, null, null, null, false, new ProjectFieldSettings(null, null), null);
        var fieldWithValue = new FieldWithValue(field, null);
        _ = Should.NotThrow(fieldWithValue.GetCurrentFee);
    }
}
