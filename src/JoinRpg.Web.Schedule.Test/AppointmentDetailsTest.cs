namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Оверлей деталей заполняет JS (<c>appointmentClickHandler</c> в <c>FullScreen.cshtml</c>):
/// он кладёт значения в элементы по id и навешивает классы <c>details-no-*</c> на внешний
/// контейнер, чтобы спрятать пустые секции. Поэтому id и классы секций — контракт со
/// скриптом и с <c>AppointmentDetails.css</c>, и переименование ломает оверлей молча,
/// без ошибок в логах. Тест фиксирует этот контракт.
/// </summary>
public class AppointmentDetailsTest
{
    [Theory]
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

    [Fact]
    public void CloseButtonCallsScriptHandler()
    {
        using var ctx = new BunitContext();

        ctx.Render<AppointmentDetails>().Find("button.btn-link").GetAttribute("onclick")
            .ShouldBe("closeDetailsClickHandler()");
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
