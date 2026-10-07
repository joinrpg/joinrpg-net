using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.Fields;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Fields;

public class TimeSlotMassAddTest : BunitContext
{
    private static readonly TimeSlotMassAddViewModel Model = new(
        new ProjectFieldIdentification(new ProjectIdentification(1), 2),
        new DateOnly(2026, 7, 10),
        50);

    private static TimeSlotMassAddFormModel ValidForm() => new()
    {
        Prefix = "Зал",
        Date = new DateOnly(2026, 7, 10),
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(12, 0),
        SlotMinutes = 50,
        BreakMinutes = 10,
    };

    private static List<ValidationResult> Validate(TimeSlotMassAddFormModel form)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(form, new ValidationContext(form), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ValidForm_HasNoErrors() => Validate(ValidForm()).ShouldBeEmpty();

    [Fact]
    public void StartEqualsEnd_IsError()
    {
        var form = ValidForm();
        form.EndTime = form.StartTime;

        Validate(form).ShouldHaveSingleItem().MemberNames.ShouldBe([nameof(TimeSlotMassAddFormModel.EndTime)]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(TimeSlotBatch.MaxMinutes + 1, 0)]
    [InlineData(50, -1)]
    [InlineData(50, int.MaxValue)]
    public void LengthsOutOfRange_AreErrors(int slotMinutes, int breakMinutes)
    {
        var form = ValidForm();
        form.SlotMinutes = slotMinutes;
        form.BreakMinutes = breakMinutes;

        Validate(form).ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1999)]
    [InlineData(2100)]
    public void DateOutOfRange_IsError(int year)
    {
        var form = ValidForm();
        form.Date = new DateOnly(year, 7, 10);

        Validate(form).ShouldHaveSingleItem().MemberNames.ShouldBe([nameof(TimeSlotMassAddFormModel.Date)]);
    }

    [Fact]
    public void EndBeforeStart_EndsNextDay()
    {
        var form = ValidForm();
        form.EndTime = new TimeOnly(1, 0);

        form.EndsNextDay.ShouldBeTrue();
        Validate(form).ShouldBeEmpty();
    }

    /// <summary>
    /// Параметр острова сериализуется при пререндере; если он не переживёт round-trip, остров молча не отрисуется
    /// </summary>
    [Fact]
    public void ViewModel_SurvivesJsonRoundTrip()
    {
        var json = JsonSerializer.Serialize(Model);

        JsonSerializer.Deserialize<TimeSlotMassAddViewModel>(json).ShouldBe(Model);
    }

    [Fact]
    public void RendersButtonWithDefaults()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IProjectFieldOperationsClient>(new NotUsedClient());
        Services.AddSingleton<ILogger<TimeSlotMassAddButton>>(new NullLogger<TimeSlotMassAddButton>());

        var cut = Render<TimeSlotMassAddButton>(p => p.Add(x => x.Model, Model));

        cut.Markup.ShouldContain("Добавить много значений");
        cut.Find("input[name=StartTime]").GetAttribute("value").ShouldBe("10:00");
        cut.Find("input[name=EndTime]").GetAttribute("value").ShouldBe("22:00");
        cut.Find("input[type=date]").GetAttribute("value").ShouldBe("2026-07-10");
    }

    private class NotUsedClient : IProjectFieldOperationsClient
    {
        public Task Delete(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task DeleteVariant(ProjectFieldVariantIdentification variantId) => throw new NotSupportedException();
        public Task DeleteUnusedVariants(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task CreateTimeSlots(TimeSlotMassAddRequest request) => throw new NotSupportedException();
        public Task SortTimeSlotsByStartTime(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task SortVariantsByLabel(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task CreateVariants(FieldValuesMassAddRequest request) => throw new NotSupportedException();
    }
}
