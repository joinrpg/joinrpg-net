using JoinRpg.DomainTypes.Plots;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.CharacterGroups.Test;

/// <summary>
/// Регрессия: любой сбой загрузки оставлял остров в «Идет загрузка...» навсегда. Так уже дважды
/// уезжало на прод незамеченным — сервер отдаёт HTTP 200, падение происходит на клиенте
/// (баг #4883 — JsonException на UserId(-1); см. также docs/adr011-roles-grid-payload.md).
/// </summary>
public class ProjectRoleGridLoadFailureTest
{
    private static readonly ProjectRolesListIdentification RolesListId = new(new ProjectIdentification(1), 2);

    private sealed class ThrowingClient : IProjectRoleGridClient
    {
        public Task<ProjectRoleGridViewResult> GetRoleGrid(ProjectRolesListIdentification id)
            => throw new InvalidOperationException("Could not parse 'UserId(-1)' as UserIdentification");

        public Task<ProjectRoleGridViewResult> GetRoleGridForPlot(PlotElementIdentification plotElementId)
            => throw new InvalidOperationException("Could not parse 'UserId(-1)' as UserIdentification");

        public Task<ProjectRoleGridViewResult> GetClassicRoleGrid(ProjectIdentification projectId, CharacterGroupIdentification? groupId, bool hotOnly = false)
            => throw new InvalidOperationException("Could not parse 'UserId(-1)' as UserIdentification");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IProjectRoleGridClient>(new ThrowingClient());
        // Кнопка «Печать» в шапке резолвит URL через IProjectUriLocator (ADR029); сам тест
        // URL не проверяет — достаточно заглушки, чтобы property-инъекция компонента не падала.
        ctx.Services.AddSingleton<IProjectUriLocator>(new StubProjectUriLocator());
        // Остров инжектит PersistentComponentState; в тесте берём его у менеджера — состояние
        // пустое, то есть компонент пойдёт в клиент, как и на живой странице.
        ctx.Services.AddSingleton(new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance).State);
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        return ctx;
    }

    [Fact]
    public void LoadFailure_ShowsErrorInsteadOfEndlessLoading()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<ProjectRoleGrid.ProjectRoleGrid>(p => p.Add(x => x.RolesListId, RolesListId));

        cut.Markup.ShouldContain("Не удалось загрузить сетку ролей");
        cut.Markup.ShouldNotContain("Идет загрузка");
    }

    /// <summary>Минимальная заглушка IProjectUriLocator — тест проверяет не URL, а поведение при сбое загрузки.</summary>
    private sealed class StubProjectUriLocator : IProjectUriLocator
    {
        private static Uri Stub(ProjectIdentification projectId, string suffix) => new($"https://example.org/{projectId.Value}/{suffix}");

        public Uri GetMyClaimUri(ProjectIdentification projectId) => Stub(projectId, "my-claim");
        public Uri GetAddClaimUri(ProjectIdentification projectId) => Stub(projectId, "add-claim");
        public Uri GetCreatePlotUri(ProjectIdentification projectId) => Stub(projectId, "create-plot");
        public Uri GetRolesListUri(ProjectIdentification projectId) => Stub(projectId, "roleslist");
        public Uri GetCaptainCabinetUri(ProjectIdentification projectId) => Stub(projectId, "captain");
        public Uri GetCreateCharacterUri(ProjectIdentification projectId) => Stub(projectId, "create-character");
        public Uri GetMassMailUri(ProjectIdentification projectId, IReadOnlyCollection<ClaimIdentification> claimIds) => Stub(projectId, "mass-mail");
        public Uri GetPrintCharactersUri(ProjectIdentification projectId, IReadOnlyCollection<CharacterIdentification> characterIds) => Stub(projectId, "print");
        public Uri GetPrintBadgesUri(ProjectIdentification projectId, IReadOnlyCollection<CharacterIdentification> characterIds) => Stub(projectId, "badges");
        public Uri GetRoleGridPrintUri(ProjectRolesListIdentification rolesListId) => Stub(rolesListId.ProjectId, "roleslist-print");
        public Uri GetClassicRoleGridPrintUri(ProjectIdentification projectId, CharacterGroupIdentification? groupId, bool hotOnly) => Stub(projectId, "classic-print");
    }
}
