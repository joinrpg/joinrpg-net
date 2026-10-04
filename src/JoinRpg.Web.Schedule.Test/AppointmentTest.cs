using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Карточка мероприятия переехала из cshtml-партиала <c>_AppointmentPartial</c> в razor-компонент
/// без изменения поведения. Тесты фиксируют разметку, от которой зависят CSS и JS расписания:
/// набор классов и data-атрибуты, из которых
/// полноэкранный режим собирает оверлей деталей.
/// </summary>
public class AppointmentTest
{
    private static readonly ProjectIdentification ProjectId = new(1620);
    private static readonly CharacterIdentification CharacterId = new(ProjectId, 999);

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<Web.ProjectCommon.ICharacterUriLocator>(new FakeCharacterUriLocator());
        ctx.Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkUriLocator());
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return ctx;
    }

    private static AppointmentViewModel Model(
        AppointmentErrorType? errorType = null,
        bool allRooms = false,
        bool hasMasterAccess = false,
        int usersCount = 1,
        MarkupString description = default,
        IReadOnlyCollection<TableHeaderViewModel>? rooms = null,
        IReadOnlyCollection<TableHeaderViewModel>? slots = null)
        => new()
        {
            DisplayName = "Мастер-класс по фехтованию",
            CharacterId = CharacterId,
            Users = [.. Enumerable.Range(1, usersCount).Select(
                i => new UserLinkViewModel(new UserIdentification(i), $"Ведущий {i}", ViewMode.Show))],
            Description = description,
            ErrorType = errorType,
            AllRooms = allRooms,
            HasMasterAccess = hasMasterAccess,
            Rooms = rooms ?? [],
            Slots = slots ?? [],
        };

    private static TableHeaderViewModel Header(int id, string name)
        => new() { Id = Variant(id), Name = name, Description = new MarkupString("") };

    private static ProjectFieldVariantIdentification Variant(int id)
        => new(new ProjectFieldIdentification(ProjectId, 7), id);

    private static IRenderedComponent<Appointment> Render(BunitContext ctx, AppointmentViewModel model)
        => ctx.Render<Appointment>(p => p.Add(x => x.Model, model));

    /// <summary>
    /// Карточка не знает, где лежит: место в сетке задаёт ячейка <c>Scheduler</c>,
    /// а размер в списках ошибок — стили <c>Intersections</c> и <c>NotAllocated</c>.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(AppointmentErrorType.Intersection)]
    [InlineData(AppointmentErrorType.NotLocated)]
    public void HasNoInlineStyle(AppointmentErrorType? errorType)
    {
        using var ctx = CreateContext();

        Render(ctx, Model(errorType: errorType)).Find("div.scheduler-appointment")
            .HasAttribute("style").ShouldBeFalse();
    }

    [Fact]
    public void WithUsers_GetsHasUsersClass()
    {
        using var ctx = CreateContext();

        var classes = Render(ctx, Model(usersCount: 2)).Find("div.scheduler-appointment").GetAttribute("class");

        classes.ShouldBe("scheduler-appointment appointment-has-users");
    }

    [Fact]
    public void WithoutUsers_GetsNoUsersClass()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(usersCount: 0));

        cut.Find("div.scheduler-appointment").GetAttribute("class")
            .ShouldBe("scheduler-appointment appointment-no-users");
    }

    [Fact]
    public void Intersection_IsMarkedByClassAndTitle()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(errorType: AppointmentErrorType.Intersection));

        cut.Find("div.scheduler-appointment").GetAttribute("class")
            .ShouldBe("scheduler-appointment appointment-intersection appointment-has-users");
        cut.Find("div.appointment-interior").GetAttribute("title")
            .ShouldBe("Мастер-класс по фехтованию (имеются пересечения)");
    }

    [Fact]
    public void AllRooms_IsMarkedByClass()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(allRooms: true));

        cut.Find("div.scheduler-appointment").GetAttribute("class")
            .ShouldBe("scheduler-appointment appointment-all-rooms appointment-has-users");
    }

    [Fact]
    public void WithoutError_LinkOpensInSameTab()
    {
        using var ctx = CreateContext();

        var link = Render(ctx, Model()).Find("div.appointment-header a");

        link.GetAttribute("target").ShouldBe("_self");
        link.GetAttribute("href").ShouldBe($"https://example.org/{ProjectId.Value}/character/{CharacterId.CharacterId}");
    }

    [Fact]
    public void WithError_LinkOpensInNewTabToKeepGridVisible()
    {
        using var ctx = CreateContext();

        Render(ctx, Model(errorType: AppointmentErrorType.NotLocated))
            .Find("div.appointment-header a").GetAttribute("target").ShouldBe("_blank");
    }

    [Fact]
    public void WithoutMasterAccess_HasNoEditLink()
    {
        using var ctx = CreateContext();

        Render(ctx, Model()).FindAll("div.appointment-header a").Count.ShouldBe(1);
    }

    [Fact]
    public void WithMasterAccess_HasEditLink()
    {
        using var ctx = CreateContext();

        var links = Render(ctx, Model(hasMasterAccess: true)).FindAll("div.appointment-header a");

        links.Count.ShouldBe(2);
        links[1].GetAttribute("href")
            .ShouldBe($"https://example.org/{ProjectId.Value}/character/{CharacterId.CharacterId}/edit");
    }

    [Fact]
    public void NotLocated_ListsRoomsAndSlots()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(
            errorType: AppointmentErrorType.NotLocated,
            rooms: [Header(1, "Шатёр"), Header(2, "Поляна")],
            slots: [Header(10, "10:00")]));

        cut.Find("div.appointment-rooms").TextContent.ShouldContain("Шатёр, Поляна");
        cut.Find("div.appointment-slots").TextContent.ShouldContain("10:00");
    }

    [Fact]
    public void NotLocatedWithoutRoomsAndSlots_SaysSoInRed()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(errorType: AppointmentErrorType.NotLocated));

        cut.Find("div.appointment-rooms b.text-danger").TextContent.ShouldBe("нет");
        cut.Find("div.appointment-slots b.text-danger").TextContent.ShouldBe("нет");
    }

    [Fact]
    public void NotLocatedWithAllRooms_SaysAllInsteadOfListing()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(
            errorType: AppointmentErrorType.NotLocated,
            allRooms: true,
            rooms: [Header(1, "Шатёр"), Header(2, "Поляна")]));

        cut.Find("div.appointment-rooms").TextContent.ShouldContain("все");
        cut.Find("div.appointment-rooms").TextContent.ShouldNotContain("Шатёр");
    }

    /// <summary>
    /// Без деталей поверх карточки ничего нет: слой-кнопка закрыл бы ссылки на персонажа.
    /// </summary>
    [Fact]
    public void WithoutDetails_HasNoClickOverlayNorPanel()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model());

        cut.FindAll("button.appointment-click-overlay").ShouldBeEmpty();
        cut.FindAll("[popover]").ShouldBeEmpty();
    }

    /// <summary>
    /// Панель открывает браузер по popovertarget — id кнопки и панели должны совпадать,
    /// иначе клик молча ничего не делает.
    /// </summary>
    [Fact]
    public void WithDetails_ClickOverlayOpensItsPanel()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<Appointment>(p => p.Add(x => x.Model, Model()).Add(x => x.WithDetails, true));

        var target = cut.Find("button.appointment-click-overlay").GetAttribute("popovertarget");
        target.ShouldBe($"appointment{CharacterId.CharacterId}-0-0-details");
        cut.Find($"#{target}").HasAttribute("popover").ShouldBeTrue();
    }
}
