using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// Клиентский (WASM) <see cref="IProjectUriLocator"/> отдаёт относительные URI, а у них
/// <see cref="Uri.AbsoluteUri"/> бросает исключение — остров с такой кнопкой падал бы при рендере
/// молча (сервер отдаёт 200). Кнопки должны принимать оба вида URI.
/// </summary>
public class ProjectClaimButtonsRelativeUriTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private sealed class RelativeLocator : IProjectUriLocator
    {
        private static Uri UriFor(string suffix) => new($"/1/{suffix}", UriKind.Relative);

        public Uri GetMyClaimUri(ProjectIdentification projectId) => UriFor("myclaim");
        public Uri GetAddClaimUri(ProjectIdentification projectId) => UriFor("apply");
        public Uri GetCreatePlotUri(ProjectIdentification projectId) => UriFor("plots/create");
        public Uri GetRolesListUri(ProjectIdentification projectId) => UriFor("roles");
        public Uri GetCaptainCabinetUri(ProjectIdentification projectId) => UriFor("captain");
        public Uri GetCreateCharacterUri(ProjectIdentification projectId) => UriFor("character/create");
        public Uri GetMassMailUri(ProjectIdentification projectId, IReadOnlyCollection<ClaimIdentification> claimIds) => UriFor("massmail");
        public Uri GetPrintCharactersUri(ProjectIdentification projectId, IReadOnlyCollection<CharacterIdentification> characterIds) => UriFor("print");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IProjectUriLocator>(new RelativeLocator());
        ctx.Services.AddLogging();
        return ctx;
    }

    [Fact]
    public void SendClaimButton_RelativeUri_RendersLink()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<SendClaimButton>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ProjectStatus, ProjectLifecycleStatus.ActiveClaimsOpen));

        cut.Find("a").GetAttribute("href").ShouldBe("/1/apply");
    }

    [Fact]
    public void MyClaimButton_RelativeUri_RendersLink()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<MyClaimButton>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.HasMyClaims, true)
            .Add(x => x.ProjectStatus, ProjectLifecycleStatus.ActiveClaimsOpen));

        cut.Find("a").GetAttribute("href").ShouldBe("/1/myclaim");
    }
}
