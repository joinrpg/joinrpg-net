using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

public class ProjectProfileRequirementSettingsTest
{
    [Fact]
    public void NothingRequested_NoSensitiveData()
        => ProjectProfileRequirementSettings.AllNotRequired.SensitiveDataRequired.ShouldBeFalse();

    [Fact]
    public void OnlyBirthDateRequested_IsSensitiveData()
        => (ProjectProfileRequirementSettings.AllNotRequired with { RequireBirthDate = MandatoryStatus.Recommended })
            .SensitiveDataRequired.ShouldBeTrue();
}
