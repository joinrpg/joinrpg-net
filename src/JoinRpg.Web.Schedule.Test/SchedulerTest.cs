using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Сетка расписания — один CSS grid: первая строка и первая колонка — шапка, остальное — поле
/// карточек. Тесты фиксируют то, на что опирается CSS: число колонок и строк, которое компонент
/// передаёт переменными, и место каждой карточки и заголовка в сетке.
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

    private static AppointmentViewModel Appointment(int id, int roomIndex = 0, int roomCount = 1, int slotIndex = 0, int slotsCount = 1)
        => new()
        {
            DisplayName = $"Мероприятие {id}",
            CharacterId = new CharacterIdentification(ProjectId, id),
            Users = [],
            RoomIndex = roomIndex,
            RoomCount = roomCount,
            TimeSlotIndex = slotIndex,
            TimeSlotsCount = slotsCount,
        };

    private static SchedulePageViewModel Model(int columns = 3, int rows = 2, int appointments = 0)
        => new()
        {
            ProjectId = ProjectId,
            DisplayName = "Тестовая песочница",
            Columns = [.. Enumerable.Range(1, columns).Select(i => Header(i, $"Комната {i}"))],
            Rows = [.. Enumerable.Range(1, rows).Select(i => Header(100 + i, $"Слот {i}"))],
            Appointments = [.. Enumerable.Range(1, appointments).Select(i => Appointment(i))],
            NotAllocated = [],
            Intersections = [],
        };

    [Fact]
    public void PassesGridSizeToCss()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model(columns: 3, rows: 2)));

        cut.Find("div.scheduler-scrollable").GetAttribute("style")
            .ShouldBe("--scheduler-columns: 3; --scheduler-rows: 2");
    }

    /// <summary>
    /// Первая колонка сетки — заголовки слотов, поэтому комнаты начинаются со второй.
    /// </summary>
    [Fact]
    public void RendersColumnHeaderPerRoom()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model(columns: 3)));

        var headers = cut.FindAll("div.scheduler-header-column");
        headers.Count.ShouldBe(3);
        headers[0].Id.ShouldBe("project-room1");
        headers[0].TextContent.ShouldBe("Комната 1");
        headers[0].GetAttribute("style").ShouldBe("grid-column: 2");
        headers[2].GetAttribute("style").ShouldBe("grid-column: 4");
    }

    /// <summary>
    /// Первая строка сетки — заголовки комнат, поэтому слоты начинаются со второй.
    /// </summary>
    [Fact]
    public void RendersRowHeaderPerTimeSlot()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model(rows: 2)));

        var headers = cut.FindAll("div.scheduler-header-row");
        headers.Count.ShouldBe(2);
        headers[0].Id.ShouldBe("project-slot101");
        headers[0].GetAttribute("style").ShouldBe("grid-row: 2");
        headers[1].GetAttribute("style").ShouldBe("grid-row: 3");
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

        cut.Find("div.scheduler-header-column").GetAttribute("title").ShouldBe("<p>Большой</p>");
    }

    [Fact]
    public void RendersAppointmentPerScheduledItem()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model(appointments: 4)));

        cut.FindAll("div.scheduler-cell > div.scheduler-appointment").Count.ShouldBe(4);
    }

    /// <summary>
    /// Карточка занимает свои комнаты и слоты: индексы в модели с нуля, а в сетке
    /// первая строка и колонка — шапка.
    /// </summary>
    [Fact]
    public void PlacesAppointmentOverItsRoomsAndSlots()
    {
        using var ctx = CreateContext();
        var model = Model(columns: 4, rows: 5);
        model.Appointments = [Appointment(1, roomIndex: 1, roomCount: 3, slotIndex: 2, slotsCount: 2)];

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, model));

        cut.Find("div.scheduler-cell").GetAttribute("style")
            .ShouldBe("grid-column: 3 / span 3; grid-row: 4 / span 2");
    }

    /// <summary>
    /// Режим задаёт модификатор на корне сетки: от него в scoped CSS зависят высота, рамка
    /// и то, открываются ли детали по клику на карточку.
    /// </summary>
    [Theory]
    [InlineData(false, "scheduler-embedded")]
    [InlineData(true, "scheduler-fullscreen")]
    public void MarksRootWithMode(bool fullScreen, string modeClass)
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model()).Add(x => x.FullScreen, fullScreen));

        cut.Find("#scheduler").ClassList.ShouldBe(["scheduler", modeClass], ignoreOrder: true);
    }

    /// <summary>
    /// Оверлей деталей накрывает сетку и позиционируется от её корня, поэтому живёт внутри неё,
    /// и только в полноэкранном режиме: на обычной странице скрипта, который его заполняет, нет.
    /// </summary>
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void RendersDetailsOverlayOnlyInFullScreen(bool fullScreen, int expectedCount)
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Scheduler>(p => p.Add(x => x.Model, Model()).Add(x => x.FullScreen, fullScreen));

        cut.FindAll("#scheduler #scheduler-overlay").Count.ShouldBe(expectedCount);
    }
}
