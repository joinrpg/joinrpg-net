using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Регрессия переноса из cshtml в компоненты: Blazor по умолчанию вырезает текстовые узлы
/// из одних пробелов, стоящие рядом с блоками кода, — и разметка склеивалась
/// («МК(ред.)», «Комнаты:Шатёр», «ПервыйВторой»). Поэтому разделители в <c>Appointment</c>
/// заданы явно: внутри текста подписи («Комнаты: ») или выводом выражения (<c>Space</c>) —
/// статическую разметку из одних пробелов Blazor вырезает, включая
/// <c>&lt;text&gt; &lt;/text&gt;</c>. В карточке склейка имён не видна
/// (<c>.appointment-users</c> — flex), зато видна в оверлее деталей: туда innerHTML
/// копируется в обычный div.
/// </summary>
public class AppointmentWhitespaceTest
{
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
        bool hasMasterAccess = false,
        bool allRooms = false,
        int usersCount = 2,
        IReadOnlyCollection<TableHeaderViewModel>? rooms = null,
        IReadOnlyCollection<TableHeaderViewModel>? slots = null)
        => new(() => new Rect { Left = 0, Top = 0, Width = 225, Height = 90 })
        {
            DisplayName = "Мастер-класс",
            CharacterId = CharacterId,
            Users = [.. Enumerable.Range(1, usersCount).Select(
                i => new UserLinkViewModel(new UserIdentification(i), $"Ведущий {i}", ViewMode.Show))],
            ErrorMode = errorType.HasValue,
            ErrorType = errorType,
            AllRooms = allRooms,
            HasMasterAccess = hasMasterAccess,
            Rooms = rooms ?? [],
            Slots = slots ?? [],
        };

    private static readonly ProjectIdentification ProjectId = new(1620);
    private static readonly CharacterIdentification CharacterId = new(ProjectId, 7);

    private static ProjectFieldVariantIdentification Variant(int id)
        => new(new ProjectFieldIdentification(ProjectId, 7), id);

    private static TableHeaderViewModel Header(int id, string name)
        => new() { Id = Variant(id), Name = name, Description = new MarkupString("") };

    /// <summary>
    /// В HTML любая последовательность пробелов и переводов строк — один пробел,
    /// поэтому сравниваем текст именно так: нам важно наличие разделителя, а не его вид.
    /// </summary>
    private static string Text(BunitContext ctx, AppointmentViewModel model, string selector)
        => string.Join(' ', ctx.Render<Appointment>(p => p.Add(x => x.Model, model))
            .Find(selector).TextContent
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [Fact]
    public void EditLinkIsSeparatedFromTitle()
    {
        using var ctx = CreateContext();

        Text(ctx, Model(hasMasterAccess: true), "div.appointment-header")
            .ShouldBe("Мастер-класс (ред.)");
    }

    [Fact]
    public void RoomsAreSeparatedFromLabel()
    {
        using var ctx = CreateContext();

        Text(ctx, Model(
                errorType: AppointmentErrorType.NotLocated,
                rooms: [Header(1, "Шатёр"), Header(2, "Поляна")]),
            "div.appointment-rooms")
            .ShouldBe("Комнаты: Шатёр, Поляна");
    }

    [Fact]
    public void SlotsAreSeparatedFromLabel()
    {
        using var ctx = CreateContext();

        Text(ctx, Model(
                errorType: AppointmentErrorType.NotLocated,
                slots: [Header(10, "10:00"), Header(11, "11:00")]),
            "div.appointment-slots")
            .ShouldBe("Слоты: 10:00, 11:00");
    }

    [Fact]
    public void MissingRoomsAndSlotsAreSeparatedFromLabel()
    {
        using var ctx = CreateContext();

        Text(ctx, Model(errorType: AppointmentErrorType.NotLocated), "div.appointment-rooms")
            .ShouldBe("Комнаты: нет");
        Text(ctx, Model(errorType: AppointmentErrorType.NotLocated), "div.appointment-slots")
            .ShouldBe("Слоты: нет");
    }

    [Fact]
    public void AllRoomsIsSeparatedFromLabel()
    {
        using var ctx = CreateContext();

        Text(ctx, Model(
                errorType: AppointmentErrorType.NotLocated,
                allRooms: true,
                rooms: [Header(1, "Шатёр")]),
            "div.appointment-rooms")
            .ShouldBe("Комнаты: все");
    }

    [Fact]
    public void UserNamesAreSeparatedFromEachOther()
    {
        using var ctx = CreateContext();

        Text(ctx, Model(usersCount: 2), "#appointment7-users")
            .ShouldBe("Ведущий 1 Ведущий 2");
    }

    [Fact]
    public void InGrid_FlagAttributesAreFalse()
    {
        using var ctx = CreateContext();

        var root = ctx.Render<Appointment>(p => p.Add(x => x.Model, Model()))
            .Find("div.scheduler-appointment");

        root.GetAttribute("no-users").ShouldBe("False");
        root.GetAttribute("all-rooms").ShouldBe("False");
        root.GetAttribute("error-mode").ShouldBe("False");
    }
}
