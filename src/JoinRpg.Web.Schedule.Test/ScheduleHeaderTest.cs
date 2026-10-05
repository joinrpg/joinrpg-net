using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Шапка обычной страницы расписания: ссылка на iCal строится локатором (её копируют
/// в календарь, роут менять нельзя), справка ведёт на инструкцию в документации.
/// </summary>
public class ScheduleHeaderTest
{
    private sealed class FakeScheduleUriLocator : IScheduleUriLocator
    {
        public Uri GetScheduleUri(ProjectIdentification projectId)
            => new($"https://example.org/{projectId.Value}/schedule");

        public Uri GetIcalUri(ProjectIdentification projectId)
            => new($"https://example.org/{projectId.Value}/schedule/ical");

        public Uri GetFullScreenUri(ProjectIdentification projectId)
            => new($"https://example.org/{projectId.Value}/schedule/full");
    }

    [Fact]
    public void LinksToInstructionIcalAndFullScreen()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IScheduleUriLocator>(new FakeScheduleUriLocator());

        var hrefs = ctx.Render<ScheduleHeader>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1620)))
            .FindAll("a").Select(a => a.GetAttribute("href")).ToList();

        hrefs.ShouldBe([
            "https://docs.joinrpg.ru/schedule/add_to_calendar.html",
            "https://example.org/1620/schedule/ical",
            "https://example.org/1620/schedule/full",
        ]);
    }
}
