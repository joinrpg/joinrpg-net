using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Шапка полноэкранного расписания — единственный выход из него в обычный вид,
/// поэтому ссылка важна. Строится локатором, а не руками.
/// </summary>
public class FullScreenHeaderTest
{
    private sealed class FakeScheduleUriLocator : IScheduleUriLocator
    {
        public Uri GetScheduleUri(ProjectIdentification projectId)
            => new($"https://example.org/{projectId.Value}/schedule");
    }

    [Fact]
    public void LinksBackToNormalSchedule()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IScheduleUriLocator>(new FakeScheduleUriLocator());

        var link = ctx.Render<FullScreenHeader>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1620)))
            .Find("a");

        link.GetAttribute("href").ShouldBe("https://example.org/1620/schedule");
        link.GetAttribute("class").ShouldBe("header-link");
        link.TextContent.ShouldBe("Расписание в обычном виде...");
    }
}
