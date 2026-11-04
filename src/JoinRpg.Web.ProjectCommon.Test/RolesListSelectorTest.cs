using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.ProjectMetadata;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

public class RolesListSelectorTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private sealed class FakeSelectorClient : IRolesListSelectorClient
    {
        public int Calls { get; private set; }

        public Task<ProjectRolesListDto[]> GetRolesListsList(ProjectIdentification projectId)
        {
            Calls++;
            return Task.FromResult<ProjectRolesListDto[]>(
            [
                new ProjectRolesListDto(new ProjectRolesListIdentification(projectId, 10), "Все роли"),
                new ProjectRolesListDto(new ProjectRolesListIdentification(projectId, 20), "Горячие роли"),
            ]);
        }
    }

    private static (BunitContext Context, FakeSelectorClient Client) CreateContext()
    {
        var ctx = new BunitContext();
        var client = new FakeSelectorClient();
        ctx.Services.AddSingleton<IRolesListSelectorClient>(client);
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Non-interactive: так TypedSelector рендерится в MVC-формах (render-mode="Static").
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return (ctx, client);
    }

    [Fact]
    public void RendersSavedRolesLists()
    {
        var (ctx, client) = CreateContext();
        using var _ = ctx;

        var cut = ctx.Render<RolesListSelector>(p => p.Add(x => x.ProjectId, ProjectId));

        cut.Markup.ShouldContain("Все роли");
        cut.Markup.ShouldContain("Горячие роли");
        client.Calls.ShouldBe(1);
    }
}
