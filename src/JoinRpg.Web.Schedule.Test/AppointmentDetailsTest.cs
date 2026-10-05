using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Панель деталей мероприятия рендерится на сервере из модели карточки и открывается
/// браузером (popover). Раньше её заполнял JS из data-атрибутов карточки; теперь пустые
/// секции просто не выводятся.
/// </summary>
public class AppointmentDetailsTest
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

    private static TableHeaderViewModel Header(int id, string name)
        => new() { Id = new(new ProjectFieldIdentification(ProjectId, 7), id), Name = name, Description = new MarkupString("") };

    private static AppointmentViewModel Model(
        AppointmentErrorType? errorType = null,
        bool allRooms = false,
        int usersCount = 1,
        string description = "",
        IReadOnlyCollection<TableHeaderViewModel>? rooms = null,
        IReadOnlyCollection<TableHeaderViewModel>? slots = null)
        => new()
        {
            DisplayName = "Мастер-класс по фехтованию",
            CharacterId = CharacterId,
            Users = [.. Enumerable.Range(1, usersCount).Select(
                i => new UserLinkViewModel(new UserIdentification(i), $"Ведущий {i}", ViewMode.Show))],
            Description = new MarkupString(description),
            ErrorType = errorType,
            AllRooms = allRooms,
            Rooms = rooms ?? [],
            Slots = slots ?? [],
        };

    private static IRenderedComponent<AppointmentDetails> Render(BunitContext ctx, AppointmentViewModel model)
        => ctx.Render<AppointmentDetails>(p => p.Add(x => x.Model, model));

    [Fact]
    public void IsPopoverWithIdOfItsAppointment()
    {
        using var ctx = CreateContext();

        var panel = Render(ctx, Model()).Find("div.appointment-details-popover");

        panel.HasAttribute("popover").ShouldBeTrue();
        panel.Id.ShouldBe($"appointment{CharacterId.CharacterId}-0-0-details");
    }

    [Fact]
    public void CloseButtonHidesItsPanel()
    {
        using var ctx = CreateContext();

        var button = Render(ctx, Model()).Find(".appointment-details-header button");

        button.GetAttribute("popovertarget").ShouldBe($"appointment{CharacterId.CharacterId}-0-0-details");
        button.GetAttribute("popovertargetaction").ShouldBe("hide");
    }

    /// <summary>
    /// Крестик на кнопке закрытия должен остаться иконкой, а не исчезнуть.
    /// </summary>
    [Fact]
    public void CloseButtonShowsIcon()
    {
        using var ctx = CreateContext();

        var svg = Render(ctx, Model()).Find(".appointment-details-header button svg");

        svg.GetAttribute("class").ShouldBe("join-icon");
        // Крестик в наборе иконок называется "x" — см. JoinIconMarkup.
        svg.QuerySelector("use")!.GetAttribute("href").ShouldNotBeNull().ShouldEndWith("#x");
    }

    [Fact]
    public void ShowsEverythingAboutAppointment()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(
            errorType: AppointmentErrorType.Intersection,
            rooms: [Header(1, "Шатёр"), Header(2, "Поляна")],
            slots: [Header(10, "10:00"), Header(11, "11:00")],
            description: "<p>Приходите <b>с мечом</b></p>"));

        cut.Find(".appointment-details-title").TextContent.ShouldBe("Мастер-класс по фехтованию");
        cut.Find(".appointment-details-users a").TextContent.ShouldContain("Ведущий 1");
        cut.Find(".appointment-details-rooms > div").TextContent.ShouldBe("Шатёр, Поляна");
        cut.Find(".appointment-details-slots > div").TextContent.ShouldBe("10:00, 11:00");
        cut.Find(".appointment-details-info > div").InnerHtml.ShouldBe("<p>Приходите <b>с мечом</b></p>");
    }

    /// <summary>
    /// Текст под заголовком «Проблемы» виден пользователю и должен быть по-русски. У обоих
    /// значений enum обязан быть <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>: без него <c>GetDisplayName()</c>
    /// возвращает имя значения, и мастер видел бы «NotLocated».
    /// </summary>
    [Theory]
    [InlineData(AppointmentErrorType.NotLocated, "Не размещено в сетке расписания")]
    [InlineData(AppointmentErrorType.Intersection, "Пересечение с другими мероприятиями")]
    public void ErrorText_IsHumanReadableRussian(AppointmentErrorType errorType, string expected)
    {
        using var ctx = CreateContext();

        Render(ctx, Model(errorType: errorType)).Find(".appointment-details-errors > div")
            .TextContent.ShouldBe(expected);
    }

    [Fact]
    public void EmptySectionsAreNotRendered()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(usersCount: 0));

        cut.FindAll(".appointment-details").ShouldBeEmpty();
    }

    /// <summary>
    /// Мероприятие на все комнаты не перечисляет их: список был бы просто списком всех комнат.
    /// </summary>
    [Fact]
    public void AllRooms_HidesRoomList()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(allRooms: true, rooms: [Header(1, "Шатёр")]));

        cut.FindAll(".appointment-details-rooms").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null, false, 1, "appointment-details-popover")]
    [InlineData(AppointmentErrorType.Intersection, false, 1, "appointment-details-popover details-error")]
    [InlineData(null, true, 0, "appointment-details-popover details-all-rooms details-no-users")]
    public void HeaderColorFollowsAppointment(AppointmentErrorType? errorType, bool allRooms, int usersCount, string expected)
    {
        using var ctx = CreateContext();

        Render(ctx, Model(errorType: errorType, allRooms: allRooms, usersCount: usersCount))
            .Find("div.appointment-details-popover").GetAttribute("class").ShouldBe(expected);
    }

    [Fact]
    public void LinkToSiteOpensInNewTab()
    {
        using var ctx = CreateContext();

        var link = Render(ctx, Model()).Find(".appointment-details-footer a");

        link.GetAttribute("href").ShouldBe($"https://example.org/{ProjectId.Value}/character/{CharacterId.CharacterId}");
        link.GetAttribute("target").ShouldBe("_blank");
    }
}
