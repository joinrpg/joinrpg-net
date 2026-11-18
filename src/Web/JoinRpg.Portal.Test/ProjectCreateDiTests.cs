using JoinRpg.Web.Games.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

public class ProjectCreateDiTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
{
    [Fact]
    public void IProjectCreateClient_Resolves_From_DI()
    {
        // CreateProjectPanel рендерится в режиме WebAssemblyPrerendered — на сервере он резолвит
        // IProjectCreateClient из этого же контейнера ещё до того, как загрузится WASM-клиент.
        // Если сервис зарегистрирован только в Blazor.Client (для HTTP после гидратации), но не
        // здесь, страница падает с ComponentNotRegisteredException при первом же обращении к сервису
        // из OnInitialized/OnInitializedAsync.
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IProjectCreateClient>();
        service.ShouldNotBeNull("IProjectCreateClient should resolve from DI container (needed for server-side prerendering of CreateProjectPanel)");
    }
}
