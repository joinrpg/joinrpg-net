using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

public class TimeSlotFieldVariantTest
{
    private static TimeSlotFieldVariant MakeVariant(string? programmaticValue)
        => new(
            new ProjectFieldVariantIdentification(new ProjectFieldIdentification(ProjectInfoFixture.ProjectId, 1), 1),
            "Слот",
            Price: 0,
            IsPlayerSelectable: true,
            IsActive: true,
            CharacterGroupId: null,
            Description: null,
            MasterDescription: null,
            programmaticValue,
            wasEverUsed: false,
            parentFieldName: "Слот",
            projectTimeZone: ProjectInfoFixture.ProjectTimeZone);

    [Fact]
    public void ValidValue_IsRead()
        => MakeVariant("""{"StartTime":"2026-07-10T10:00:00","TimeSlotInMinutes":50}""")
            .StartTime.ShouldBe(new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.FromHours(3)));

    /// <summary>
    /// Вариант строится при загрузке метаданных проекта: битое значение не должно ронять весь проект
    /// </summary>
    [Theory]
    [InlineData("""{"StartTime":"0001-01-01T00:00:00","TimeSlotInMinutes":50}""")]
    [InlineData("""{"StartTime":"0001-01-01T00:00:00+03:00","TimeSlotInMinutes":50}""")]
    [InlineData("""{"StartTime":"9999-12-31T23:00:00","TimeSlotInMinutes":120}""")]
    [InlineData("""{"TimeSlotInMinutes":50}""")]
    [InlineData("""{"StartTime":"мусор","TimeSlotInMinutes":50}""")]
    [InlineData("""{"StartTime":123,"TimeSlotInMinutes":50}""")]
    [InlineData("""{"StartTime":"2026-07-10T10:00:00","TimeSlotInMinutes":-2000000000}""")]
    [InlineData("""{"StartTime":"2026-07-10T10:00:00","TimeSlotInMinutes":0}""")]
    [InlineData("не JSON")]
    [InlineData("null")]
    public void BrokenValue_FallsBackToDefault(string programmaticValue)
    {
        var variant = MakeVariant(programmaticValue);

        variant.TimeSlotOptions.HasValidStartTime.ShouldBeTrue();
        variant.TimeSlotOptions.TimeSlotInMinutes.ShouldBe(50);
    }
}
