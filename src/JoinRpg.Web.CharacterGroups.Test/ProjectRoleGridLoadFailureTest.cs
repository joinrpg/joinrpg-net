using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
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

        public Task<ProjectRoleGridViewResult> GetClassicRoleGrid(ProjectIdentification projectId, CharacterGroupIdentification? groupId, bool hotOnly = false)
            => throw new InvalidOperationException("Could not parse 'UserId(-1)' as UserIdentification");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IProjectRoleGridClient>(new ThrowingClient());
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
}
