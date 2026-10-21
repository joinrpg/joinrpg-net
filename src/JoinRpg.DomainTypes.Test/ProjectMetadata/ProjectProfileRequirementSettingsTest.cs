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

    private static readonly ProjectProfileRequirementSettings PassportOnly
        = ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Recommended };

    [Fact]
    public void AddedBirthDate_IsNewSensitiveData()
        => (PassportOnly with { RequireBirthDate = MandatoryStatus.Recommended })
            .RequestsNewSensitiveDataComparedTo(PassportOnly).ShouldBeTrue();

    [Fact]
    public void SameOrFewerRequests_IsNotNewSensitiveData()
    {
        PassportOnly.RequestsNewSensitiveDataComparedTo(PassportOnly).ShouldBeFalse();
        ProjectProfileRequirementSettings.AllNotRequired.RequestsNewSensitiveDataComparedTo(PassportOnly).ShouldBeFalse();
    }

    // Повышение до Required — не новые данные: согласие давалось на то же поле.
    [Fact]
    public void StricterSameField_IsNotNewSensitiveData()
        => (PassportOnly with { RequirePassport = MandatoryStatus.Required })
            .RequestsNewSensitiveDataComparedTo(PassportOnly).ShouldBeFalse();
}
