namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Оверлей деталей заполняет JS (<c>appointmentClickHandler</c> в <c>FullScreen.cshtml</c>):
/// он кладёт значения в элементы по id и навешивает классы <c>details-no-*</c> на внешний
/// контейнер, чтобы спрятать пустые секции. Поэтому id и классы секций — контракт со
/// скриптом и с <c>AppointmentDetails.razor.css</c>, и переименование ломает оверлей молча,
/// без ошибок в логах. Тест фиксирует этот контракт.
/// </summary>
public class AppointmentDetailsTest
{
    [Theory]
    [InlineData("scheduler-overlay")]
    [InlineData("scheduler-details")]
    [InlineData("details-title")]
    [InlineData("details-problems")]
    [InlineData("details-users")]
    [InlineData("details-rooms")]
    [InlineData("details-slots")]
    [InlineData("details-info")]
    [InlineData("details-link")]
    public void KeepsElementIdScriptFillsIn(string id)
    {
        using var ctx = new BunitContext();

        ctx.Render<AppointmentDetails>().Find($"#{id}").ShouldNotBeNull();
    }

    [Theory]
    [InlineData("appointment-details-errors")]
    [InlineData("appointment-details-users")]
    [InlineData("appointment-details-rooms")]
    [InlineData("appointment-details-slots")]
    [InlineData("appointment-details-info")]
    public void KeepsSectionClassCssHidesByDetailsNoMarkers(string className)
    {
        using var ctx = new BunitContext();

        ctx.Render<AppointmentDetails>().Find($"div.{className}").ShouldNotBeNull();
    }

    /// <summary>
    /// Затемнение вокруг деталей закрывает их по клику — обработчики объявлены в скрипте
    /// <c>FullScreen.cshtml</c>. Панель лежит прямо в затемнении: оно flex-контейнер и центрирует её.
    /// </summary>
    [Fact]
    public void OverlayCallsScriptHandlersAndWrapsDetails()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AppointmentDetails>();

        var overlay = cut.Find("#scheduler-overlay");
        overlay.GetAttribute("onclick").ShouldBe("overlayClickHandler(event)");
        overlay.GetAttribute("onwheel").ShouldBe("overlayWheelHandler(event)");
        cut.Find("#scheduler-overlay > #scheduler-details").ShouldNotBeNull();
    }

    [Fact]
    public void CloseButtonCallsScriptHandler()
    {
        using var ctx = new BunitContext();

        ctx.Render<AppointmentDetails>().Find("button.btn-link").GetAttribute("onclick")
            .ShouldBe("closeDetailsClickHandler()");
    }

    /// <summary>
    /// Единственное, что в этом переносе менялось по существу: тег-хелпер
    /// <c>&lt;join-icon&gt;</c> заменён компонентом <c>JoinIcon</c>. Крестик на кнопке
    /// закрытия должен остаться иконкой, а не исчезнуть.
    /// </summary>
    [Fact]
    public void CloseButtonShowsIcon()
    {
        using var ctx = new BunitContext();

        var svg = ctx.Render<AppointmentDetails>().Find("button.btn-link svg");

        svg.GetAttribute("class").ShouldBe("join-icon");
        // Крестик в наборе иконок называется "x" — см. JoinIconMarkup.
        svg.QuerySelector("use")!.GetAttribute("href").ShouldNotBeNull().ShouldEndWith("#x");
    }

    [Fact]
    public void LinkToSiteOpensInNewTab()
    {
        using var ctx = new BunitContext();

        var link = ctx.Render<AppointmentDetails>().Find("#details-link");

        // href подставляет скрипт из details-url карточки, в разметке он заглушка.
        link.GetAttribute("href").ShouldBe("#");
        link.GetAttribute("target").ShouldBe("_blank");
    }
}
