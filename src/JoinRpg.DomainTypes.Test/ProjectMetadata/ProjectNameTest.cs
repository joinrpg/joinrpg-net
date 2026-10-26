using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

/// <summary>
/// Длина названия проверяется после обрезки пробелов: иначе «   Тест   » проходило минимум
/// в 5 символов, а сохранялось «Тест» (#5855).
/// </summary>
public class ProjectNameTest
{
    [Theory]
    [InlineData("   Тест   ")]
    [InlineData("")]
    [InlineData("    ")]
    public void TooShortAfterTrim_Throws(string value)
        => Should.Throw<ArgumentException>(() => (ProjectName)value);

    [Fact]
    public void TooLong_Throws()
        => Should.Throw<ArgumentException>(() => (ProjectName)new string('a', 61));

    [Fact]
    public void SpacesAreTrimmed()
        => ((ProjectName)"  Тестовый  ").Value.ShouldBe("Тестовый");

    // Старые названия в БД бывают короче и длиннее — их читают конструктором, и он не должен падать.
    [Fact]
    public void Constructor_AcceptsLegacyNames()
        => new ProjectName("Тест").Value.ShouldBe("Тест");
}
