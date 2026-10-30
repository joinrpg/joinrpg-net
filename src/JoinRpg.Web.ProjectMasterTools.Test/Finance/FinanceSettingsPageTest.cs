using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectMasterTools.Finance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Finance;

/// <summary>
/// Страница «Настройка финансов» целиком — один остров: четыре панели в одном аккордеоне,
/// имя группы уходит панелям каскадом от JoinCollapsePanelGroup.
/// </summary>
public class FinanceSettingsPageTest : BunitContext
{
    private readonly FakeFinanceSettingsClient client = new();

    public FinanceSettingsPageTest()
    {
        Services.AddSingleton<IFinanceSettingsClient>(client);
        Services.AddSingleton<IFinanceSettingsUriLocator>(new FakeUriLocator());
        Services.AddSingleton<JoinRpg.Web.ProjectCommon.IMasterClient>(new FakeMasterClient());
        _ = AddBunitPersistentComponentState();
        Services.AddScoped<JoinRpg.Common.WebComponents.SelectorListCache>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("WebAssembly", isInteractive: true));
    }

    [Fact]
    public void PageShowsAllFourPanelsInOneAccordion()
    {
        var cut = Render<FinanceSettingsPage>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        // Все четыре панели на месте и принадлежат одному аккордеону: открыта не больше одной
        cut.WaitForAssertion(() => cut.FindAll("details.join-collapse-panel__toggle").Count.ShouldBe(4));
        cut.FindAll("details[name='finance-settings']").Count.ShouldBe(4);
        cut.FindAll("details.join-collapse-panel__toggle").Select(p => p.Id).ShouldBe(
            [FinanceFeeSettingsPanel.AnchorId, FinancePaymentTypesPanel.AnchorId, FinanceGeneralSettingsPanel.AnchorId, FinanceGoogleDocsPanel.AnchorId]);
    }

    [Fact]
    public void BrokenPanelDoesNotKillOtherPanels()
    {
        // Одна ручка ответила 500 — её панель показывает ошибку, остальные работают
        client.GetFeeSettingsError = new HttpRequestException("502");

        var cut = Render<FinanceSettingsPage>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        // Панель взносов показывает ошибку, но не роняет остальные три
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось загрузить расписание взносов"));
        cut.FindAll("details.join-collapse-panel__toggle").Count.ShouldBe(4);
    }

    private sealed class FakeUriLocator : IFinanceSettingsUriLocator
    {
        public Uri GetEditPaymentTypeUri(DomainTypes.ProjectMetadata.Payments.PaymentTypeIdentification paymentTypeId)
            => new($"/{paymentTypeId.ProjectId.Value}/money/EditPaymentType?PaymentTypeId={paymentTypeId.PaymentTypeId}", UriKind.Relative);

        public Uri GetMasterSummaryExportUri(ProjectIdentification projectId, string token) => new($"https://joinrpg.ru/{projectId.Value}/money/SummaryByMaster?token={token}");
    }

    private sealed class FakeMasterClient : JoinRpg.Web.ProjectCommon.IMasterClient
    {
        public Task<List<JoinRpg.Common.PrimitiveTypes.Users.UserInfoHeader>> GetMasters(int projectId) => Task.FromResult(new List<JoinRpg.Common.PrimitiveTypes.Users.UserInfoHeader>());
    }
}
