using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Сетка расписания переехала из cshtml-партиала <c>Scheduler</c> в razor-компонент.
/// Тесты фиксируют то, на что опираются CSS и JS: размеры, заданные инлайновым стилем,
/// id блоков, которые скрипт синхронизирует по прокрутке, и пустую таблицу-подложку
/// размером «строки × колонки», поверх которой абсолютно позиционируются карточки.
/// </summary>
public class SchedulerTest
{
    private static readonly ProjectIdentification ProjectId = new(1620);

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<Web.ProjectCommon.ICharacterUriLocator>(new FakeCharacterUriLocator());
        ctx.Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkUriLocator());
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return ctx;
    }

    private static ProjectFieldVariantIdentification Variant(int id)
        => new(new ProjectFieldIdentification(ProjectId, 7), id);

    private static TableHeaderViewModel Header(int id, string name, string description = "")
        => new() { Id = Variant(id), Name = name, Description = new MarkupString(description) };

    private static SchedulePageViewModel Model(int columns = 3, int rows = 2, int appointments = 0)
        => new()
        {
            ProjectId = ProjectId,
            DisplayName = "Тестовая песочница",
            Columns = [.. Enumerable.Range(1, columns).Select(i => Header(i, $"Комната {i}"))],
            Rows = [.. Enumerable.Range(1, rows).Select(i => Header(100 + i, $"Слот {i}"))],
            Appointments = [.. Enumerable.Range(1, appointments).Select(i =>
                new AppointmentViewModel(() => new Rect { Left = 0, Top = 0, Width = 225, Height = 90 })
                {
                    DisplayName = $"Мероприятие {i}",
                    CharacterId = new CharacterIdentification(ProjectId, i),
                    Users = [],
                })],
            NotAllocated = [],
            Intersections = [],
            NotScheduledProgramItems = [],
            ConflictedProgramItems = [],
            Slots = [],
        };

    [Fact]
    public void RendersColumnHeaderPerRoom()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model(columns: 3)));

        var headers = cut.FindAll("th.scheduler-header-column");
        headers.Count.ShouldBe(3);
        headers[0].Id.ShouldBe("project-room1");
        headers[0].TextContent.ShouldBe("Комната 1");
        headers[0].GetAttribute("style").ShouldBe("width: 225px; min-width: 225px");
    }

    [Fact]
    public void RendersRowHeaderPerTimeSlot()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model(rows: 2)));

        var headers = cut.FindAll("div.scheduler-header-row");
        headers.Count.ShouldBe(2);
        headers[0].Id.ShouldBe("project-slot101");
        headers[0].GetAttribute("style").ShouldBe("height: 90px");
    }

    /// <summary>
    /// Описание слота — markdown, отрендеренный в HTML. Раньше оно писалось в атрибут
    /// без экранирования, из-за чего кавычка в описании ломала разметку; теперь атрибут
    /// экранируется, а значение в DOM остаётся тем же.
    /// </summary>
    [Fact]
    public void HeaderDescriptionGoesToTitleAttribute()
    {
        using var ctx = CreateContext();
        var model = Model();
        model.Columns = [Header(1, "Шатёр", "<p>Большой</p>")];

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, model));

        cut.Find("th.scheduler-header-column").GetAttribute("title").ShouldBe("<p>Большой</p>");
    }

    [Fact]
    public void RendersEmptyGridOfRowsByColumns()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model(columns: 3, rows: 2)));

        cut.FindAll("table.scheduler-grid tbody tr").Count.ShouldBe(2);
        cut.FindAll("table.scheduler-grid tbody tr td").Count.ShouldBe(6);
        cut.FindAll("table.scheduler-grid tbody tr td")[0].GetAttribute("style")
            .ShouldBe("width: 225px; min-width: 225px; height: 90px;");
    }

    [Fact]
    public void RendersAppointmentPerScheduledItem()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model(appointments: 4)));

        cut.FindAll("div.scheduler-appointments div.scheduler-appointment").Count.ShouldBe(4);
    }

    /// <summary>
    /// Шапка и левая колонка прокручиваются скриптом по id — переименование сломает синхронизацию.
    /// </summary>
    [Fact]
    public void KeepsIdsScrollSyncScriptDependsOn()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model()));

        cut.Find("#scheduler-header-scrollable").ShouldNotBeNull();
        cut.Find("#scheduler-header-rows-scrollable").ShouldNotBeNull();
        cut.Find("div.scheduler-scrollable").GetAttribute("onscroll").ShouldBe("handleScroll(event)");
    }
}
