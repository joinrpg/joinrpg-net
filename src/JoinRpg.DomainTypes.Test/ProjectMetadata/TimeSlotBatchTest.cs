using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

public class TimeSlotBatchTest
{
    private static readonly DateOnly Date = new(2026, 7, 10);

    [Fact]
    public void EvenSplit_CreatesBackToBackSlots()
    {
        var result = TimeSlotBatch.Generate("Зал 1", Date, new(10, 0), new(12, 0), 30, 0);

        result.Select(r => r.Label).ShouldBe(["Зал 1 10:00–10:30", "Зал 1 10:30–11:00", "Зал 1 11:00–11:30", "Зал 1 11:30–12:00"]);
        result.ShouldAllBe(r => r.Options.TimeSlotInMinutes == 30);
        result[0].Options.LocalStartTime.ShouldBe(new DateTime(2026, 7, 10, 10, 0, 0));
    }

    [Fact]
    public void Tail_ThatDoesNotFit_IsDropped()
    {
        var result = TimeSlotBatch.Generate(null, Date, new(10, 0), new(11, 40), 50, 0);

        result.Select(r => r.Label).ShouldBe(["10:00–10:50", "10:50–11:40"]);

        TimeSlotBatch.Generate(null, Date, new(10, 0), new(11, 39), 50, 0)
            .Select(r => r.Label).ShouldBe(["10:00–10:50"]);
    }

    [Fact]
    public void Break_IsInsertedBetweenSlots()
    {
        var result = TimeSlotBatch.Generate("", Date, new(10, 0), new(12, 0), 50, 10);

        result.Select(r => r.Label).ShouldBe(["10:00–10:50", "11:00–11:50"]);
    }

    [Fact]
    public void EndBeforeStart_EndsNextDay()
    {
        var result = TimeSlotBatch.Generate(null, Date, new(23, 0), new(1, 0), 60, 0);

        result.Select(r => r.Label).ShouldBe(["23:00–00:00", "00:00–01:00"]);
        result[1].Options.LocalStartTime.ShouldBe(new DateTime(2026, 7, 11, 0, 0, 0));
    }

    [Fact]
    public void StartEqualsEnd_NoSlots()
        => TimeSlotBatch.Generate(null, Date, new(10, 0), new(10, 0), 30, 0).ShouldBeEmpty();

    [Theory]
    [InlineData(0, 0)]
    [InlineData(30, -1)]
    [InlineData(TimeSlotBatch.MaxMinutes + 1, 0)]
    [InlineData(60, int.MaxValue)]
    public void InvalidLengths_Throw(int slotMinutes, int breakMinutes)
        => Should.Throw<ArgumentOutOfRangeException>(
            () => TimeSlotBatch.Generate(null, Date, new(10, 0), new(11, 0), slotMinutes, breakMinutes));
}
