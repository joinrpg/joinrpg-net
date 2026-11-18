using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Виджет «Проблемы заявки» на странице заявки: мастеру — все проблемы с пометкой тех, что держат
/// регистрацию; игроку — его проблемы и намёк на скрытые мастерские поля; владельцу — кнопки профиля.
/// </summary>
public class ClaimProblemsPanelTest : BunitContext
{
    private const string AllowButtonLabel = "Разрешить доступ к защищённым данным";
    private const string AutoApproveButtonLabel = "Прими мою заявку";
    private const string ProfileLink = "/manage/setupprofile";
    private const string HiddenBlockersText = "Не заполнены скрытые мастерские поля";

    private static readonly RequestedSensitiveDataView Requested = new(Passport: true, RegistrationAddress: false, BirthDate: false);

    private static readonly ProblemViewModel MasterProblem =
        new(new ClaimProblem(ClaimProblemType.NoResponsibleMaster, ProblemSeverity.Error));

    private static readonly ProblemViewModel MissingPhone =
        new(new ProfileRelatedProblem(ClaimProblemType.MissingPhone, ProblemSeverity.Warning));

    private static readonly ProblemViewModel SensitiveDataNotAllowed =
        new(new ProfileRelatedProblem(ClaimProblemType.SensitiveDataNotAllowed, ProblemSeverity.Warning));

    public ClaimProblemsPanelTest()
    {
        Services.AddSingleton<IClaimOperationClient>(new FakeClaimOperationClient());
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<ClaimProblemsPanel> RenderPanel(
        IReadOnlyCollection<ProblemViewModel> problems,
        bool hasHiddenCheckInBlockers = false,
        RequestedSensitiveDataView? ownerActions = null,
        bool showAutoAcceptButton = false)
        => Render<ClaimProblemsPanel>(p => p
            .Add(c => c.Problems, problems)
            .Add(c => c.HasHiddenCheckInBlockers, hasHiddenCheckInBlockers)
            .Add(c => c.ProjectId, new ProjectIdentification(1))
            .Add(c => c.OwnerActions, ownerActions)
            .Add(c => c.ClaimId, new ClaimIdentification(new ProjectIdentification(1), 5))
            .Add(c => c.ShowAutoAcceptButton, showAutoAcceptButton));

    private static bool HasProfileLink(IRenderedComponent<ClaimProblemsPanel> cut)
        => cut.FindAll("a").Any(a => a.GetAttribute("href") == ProfileLink);

    [Fact]
    public void NothingToShowRendersNothing()
        => RenderPanel([]).Markup.Trim().ShouldBeEmpty();

    [Fact]
    public void CheckInBlockerIsMarked()
    {
        var cut = RenderPanel([MasterProblem, MissingPhone]);

        var items = cut.FindAll("li");
        items.Single(li => li.TextContent.Contains(MissingPhone.ProblemTypeText)).TextContent.ShouldContain("держит регистрацию");
        items.Single(li => li.TextContent.Contains(MasterProblem.ProblemTypeText)).TextContent.ShouldNotContain("держит регистрацию");
    }

    [Fact]
    public void HiddenBlockersAloneAreReported()
        => RenderPanel([], hasHiddenCheckInBlockers: true).Markup.ShouldContain(HiddenBlockersText);

    [Fact]
    public void NoHiddenBlockersNoHint()
        => RenderPanel([MissingPhone]).Markup.ShouldNotContain(HiddenBlockersText);

    [Fact]
    public void NotOwnerGetsNoButtons()
    {
        var cut = RenderPanel([MissingPhone, SensitiveDataNotAllowed]);

        HasProfileLink(cut).ShouldBeFalse();
        cut.Markup.ShouldNotContain(AllowButtonLabel);
    }

    [Fact]
    public void OwnerWithoutProfileProblemsGetsNoButtons()
        => HasProfileLink(RenderPanel([MasterProblem], ownerActions: Requested)).ShouldBeFalse();

    [Fact]
    public void OwnerWithProfileProblemGetsProfileButtonOnly()
    {
        var cut = RenderPanel([MissingPhone], ownerActions: Requested);

        HasProfileLink(cut).ShouldBeTrue();
        cut.Markup.ShouldNotContain(AllowButtonLabel);
    }

    [Fact]
    public void OwnerWithSensitiveDataNotAllowedGetsAllowButton()
    {
        var cut = RenderPanel([SensitiveDataNotAllowed], ownerActions: Requested);

        HasProfileLink(cut).ShouldBeTrue();
        cut.Markup.ShouldContain(AllowButtonLabel);
    }

    [Fact]
    public void NoAutoAcceptButtonByDefault()
        => RenderPanel([MissingPhone]).Markup.ShouldNotContain(AutoApproveButtonLabel);

    /// <summary>
    /// Кнопка одна, без проблем, — и панель ради неё всё равно рендерится: игрок дошёл до
    /// «все блокеры сняты», теперь ему должно быть удобно нажать. Пустой панель не выглядит:
    /// рядом с кнопкой — подводящая строка.
    /// </summary>
    [Fact]
    public void AutoAcceptButtonAloneRendersPanel()
    {
        var cut = RenderPanel([], showAutoAcceptButton: true);

        cut.Markup.ShouldContain(AutoApproveButtonLabel);
        cut.Markup.ShouldContain("Проблемы заявки");
        cut.Markup.ShouldContain("Всё, что мешало автоприёму, устранено");
    }

    [Fact]
    public async Task AutoAcceptButtonCallsClient()
    {
        var client = new FakeClaimOperationClient();
        Services.AddSingleton<IClaimOperationClient>(client);
        var claimId = new ClaimIdentification(new ProjectIdentification(1), 5);

        var cut = Render<ClaimProblemsPanel>(p => p
            .Add(c => c.Problems, Array.Empty<ProblemViewModel>())
            .Add(c => c.ProjectId, new ProjectIdentification(1))
            .Add(c => c.ClaimId, claimId)
            .Add(c => c.ShowAutoAcceptButton, true));

        await cut.Find("button").ClickAsync(new MouseEventArgs());

        client.AutoApproveRequestedClaim.ShouldBe(claimId);
    }
}
