using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Web.Claims.Finance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Кнопка «Как оплатить…»: список способов оплаты со ссылкой и описанием, без ссылок —
/// только описание, пустой список — кнопки нет вовсе.
/// </summary>
public class ClaimPaymentMethodsButtonTest : BunitContext
{
    public ClaimPaymentMethodsButtonTest()
    {
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var joinDialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        joinDialogModule.SetupVoid("showModal", _ => true);
        joinDialogModule.SetupVoid("closeModal", _ => true);
    }

    private static ClaimPaymentMethodView Method(string name, string? paymentLink = null, string? description = null)
        => new(name, paymentLink is null ? null : new Uri(paymentLink), PaymentLinkKind.Redirect, description);

    private IRenderedComponent<ClaimPaymentMethodsButton> RenderButton(params ClaimPaymentMethodView[] methods)
    {
        var cut = Render<ClaimPaymentMethodsButton>(parameters => parameters.Add(p => p.Methods, methods));
        cut.Find("button").Click();
        return cut;
    }

    [Fact]
    public void ShowsNameDescriptionAndLinkOfEachMethod()
    {
        var cut = RenderButton(
            Method("Карта", "https://pay.example.com/1", "Перевод на карту банка\nСкажите мастеру"),
            Method("Наличными", description: "Отдайте мастеру"));

        var dialog = cut.Find("dialog").TextContent;
        dialog.ShouldContain("Карта");
        dialog.ShouldContain("Перевод на карту банка");
        dialog.ShouldContain("Отдайте мастеру");

        // Ссылка — в новой вкладке: игрок не теряет страницу заявки
        var link = cut.Find("dialog a");
        link.GetAttribute("href").ShouldBe("https://pay.example.com/1");
        link.GetAttribute("target").ShouldBe("_blank");
        link.GetAttribute("rel").ShouldBe("noopener");
        link.TextContent.ShouldContain("Перейти к оплате");
    }

    [Fact]
    public void MethodWithoutLinkHasNoAnchor()
    {
        var cut = RenderButton(Method("Наличными", description: "Отдайте мастеру"));

        cut.FindAll("dialog a").ShouldBeEmpty();
        cut.Find("dialog").TextContent.ShouldContain("Отдайте мастеру");
    }

    [Fact]
    public void PaymentLinkEscapingIsPreserved()
    {
        // Платёжные ссылки часто содержат экранированные символы (%20, кириллица в query) —
        // в href должна попасть ссылка без изменений
        var cut = RenderButton(Method("Карта", "https://pay.example.com/?name=%D0%BF%D1%80%D0%B8%D0%B2%D0%B5%D1%82&amount=%20"));

        cut.Find("dialog a").GetAttribute("href").ShouldBe("https://pay.example.com/?name=%D0%BF%D1%80%D0%B8%D0%B2%D0%B5%D1%82&amount=%20");
    }

    [Fact]
    public void EmptyMethodsRenderNothing()
    {
        var cut = Render<ClaimPaymentMethodsButton>(parameters => parameters.Add(p => p.Methods, []));

        cut.Markup.Length.ShouldBe(0);
    }
}
