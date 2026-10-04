using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Карточка мероприятия переехала из cshtml-партиала <c>_AppointmentPartial</c> в razor-компонент
/// без изменения поведения. Тесты фиксируют разметку, от которой зависят CSS и JS расписания:
/// набор классов, абсолютное позиционирование в сетке и data-атрибуты, из которых
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
        bool errorMode = false,
        AppointmentErrorType? errorType = null,
        bool allRooms = false,
        bool hasMasterAccess = false,
        int usersCount = 1,
        MarkupString description = default,
        IReadOnlyCollection<TableHeaderViewModel>? rooms = null,
        IReadOnlyCollection<TableHeaderViewModel>? slots = null)
        => new(() => new Rect { Left = 450, Top = 180, Width = 225, Height = 90 })
        {
            DisplayName = "Мастер-класс по фехтованию",
            CharacterId = CharacterId,
            Users = [.. Enumerable.Range(1, usersCount).Select(
                i => new UserLinkViewModel(new UserIdentification(i), $"Ведущий {i}", ViewMode.Show))],
            Description = description,
            ErrorMode = errorMode,
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

    [Fact]
    public void InGrid_IsPositionedByComputedBounds()
    {
        using var ctx = CreateContext();

        var style = Render(ctx, Model()).Find("div.scheduler-appointment").GetAttribute("style");

        // Минус пиксель по ширине и высоте — чтобы соседние карточки не перекрывали рамки друг друга.
        style.ShouldBe("left: 450px; top: 180px; width: 224px; height: 89px");
    }

    [Fact]
    public void ErrorMode_IsLaidOutInFlowWithLimitedWidth()
    {
        using var ctx = CreateContext();

        var style = Render(ctx, Model(errorMode: true, errorType: AppointmentErrorType.Intersection))
            .Find("div.scheduler-appointment").GetAttribute("style");

        style.ShouldBe("max-width: 450px; height: 90px;");
    }

    [Fact]
    public void NotLocated_GetsExtraHeightForRoomsAndSlotsLines()
    {
        using var ctx = CreateContext();

        var style = Render(ctx, Model(errorMode: true, errorType: AppointmentErrorType.NotLocated))
            .Find("div.scheduler-appointment").GetAttribute("style");

        style.ShouldBe("max-width: 450px; height: 135px;");
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
        cut.Find("div.scheduler-appointment").GetAttribute("no-users").ShouldBe("True");
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
        cut.Find("div.scheduler-appointment").GetAttribute("error-mode").ShouldBe("True");
    }

    [Fact]
    public void AllRooms_IsMarkedByClassAndAttribute()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(allRooms: true));

        cut.Find("div.scheduler-appointment").GetAttribute("class")
            .ShouldBe("scheduler-appointment appointment-all-rooms appointment-has-users");
        cut.Find("div.scheduler-appointment").GetAttribute("all-rooms").ShouldBe("True");
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
            errorMode: true,
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

        var cut = Render(ctx, Model(errorMode: true, errorType: AppointmentErrorType.NotLocated));

        cut.Find("div.appointment-rooms b.text-danger").TextContent.ShouldBe("нет");
        cut.Find("div.appointment-slots b.text-danger").TextContent.ShouldBe("нет");
    }

    [Fact]
    public void NotLocatedWithAllRooms_SaysAllInsteadOfListing()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(
            errorMode: true,
            errorType: AppointmentErrorType.NotLocated,
            allRooms: true,
            rooms: [Header(1, "Шатёр"), Header(2, "Поляна")]));

        cut.Find("div.appointment-rooms").TextContent.ShouldContain("все");
        cut.Find("div.appointment-rooms").TextContent.ShouldNotContain("Шатёр");
    }

    [Fact]
    public void InGrid_HasNoErrorText()
    {
        using var ctx = CreateContext();

        Render(ctx, Model()).Find("div.scheduler-appointment").GetAttribute("errors").ShouldBe("");
    }

    /// <summary>
    /// Текст из этого атрибута JS кладёт в оверлей деталей под заголовком «Проблемы»,
    /// то есть он виден пользователю и должен быть по-русски. У обоих значений enum
    /// обязан быть <see cref="DisplayAttribute"/>: без него <c>GetDisplayName()</c>
    /// возвращает имя значения, и мастер видел бы «NotLocated».
    /// </summary>
    [Theory]
    [InlineData(AppointmentErrorType.NotLocated, "Не размещено в сетке расписания")]
    [InlineData(AppointmentErrorType.Intersection, "Пересечение с другими мероприятиями")]
    public void ErrorText_IsHumanReadableRussian(AppointmentErrorType errorType, string expected)
    {
        using var ctx = CreateContext();

        Render(ctx, Model(errorMode: true, errorType: errorType))
            .Find("div.scheduler-appointment").GetAttribute("errors").ShouldBe(expected);
    }

    /// <summary>
    /// Атрибуты читает JS полноэкранного режима, чтобы заполнить оверлей деталей,
    /// поэтому их имена и формат значений — контракт, а не деталь реализации.
    /// </summary>
    [Fact]
    public void Attributes_CarryEverythingDetailsOverlayNeeds()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, Model(
            errorType: AppointmentErrorType.Intersection,
            rooms: [Header(1, "Шатёр"), Header(2, "Поляна")],
            slots: [Header(10, "10:00"), Header(11, "11:00")],
            description: new MarkupString("<p>Приходите <b>с мечом</b></p>")));

        var root = cut.Find("div.scheduler-appointment");
        root.GetAttribute("display-name").ShouldBe("Мастер-класс по фехтованию");
        root.GetAttribute("details-url").ShouldBe($"https://example.org/{ProjectId.Value}/character/{CharacterId.CharacterId}");
        root.GetAttribute("rooms").ShouldBe("Шатёр, Поляна");
        root.GetAttribute("slots").ShouldBe("10:00, 11:00");
        root.GetAttribute("errors").ShouldBe("Пересечение с другими мероприятиями");

        cut.Find($"#appointment{CharacterId.CharacterId}-users").Children.Length.ShouldBe(1);
        cut.Find($"#appointment{CharacterId.CharacterId}-description").InnerHtml
            .ShouldBe("<p>Приходите <b>с мечом</b></p>");
    }

    [Fact]
    public void ClickOverlay_CallsHandlerWithCharacterId()
    {
        using var ctx = CreateContext();

        Render(ctx, Model()).Find("div.appointment-click-overlay").GetAttribute("onclick")
            .ShouldBe($"appointmentClickHandler({CharacterId.CharacterId})");
    }
}
