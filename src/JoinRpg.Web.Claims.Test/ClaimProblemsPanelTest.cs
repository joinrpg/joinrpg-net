using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
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
        RequestedSensitiveDataView? ownerActions = null)
        => Render<ClaimProblemsPanel>(p => p
            .Add(c => c.Problems, problems)
            .Add(c => c.HasHiddenCheckInBlockers, hasHiddenCheckInBlockers)
            .Add(c => c.ProjectId, new ProjectIdentification(1))
            .Add(c => c.OwnerActions, ownerActions));

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
}
