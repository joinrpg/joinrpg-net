using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Portal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.IntegrationTest.DependencyInjection;

/// <summary>
/// Проверяет, что все контроллеры правильно зарегистрированы в контейнере и все их зависимости резолвятся
/// </summary>
public class ResolveTest : IClassFixture<JoinApplicationFactory>
{
    private readonly IServiceProvider serviceProvider;

    public ResolveTest(JoinApplicationFactory applicationFactory)
    {
        //need to ensure that server was properly initialized before start resolving everything
        _ = applicationFactory.CreateClient();

        serviceProvider = applicationFactory.Services;
    }

    [Theory]
    [ClassData(typeof(ControllerDataSource))]
    public void AutofacControllers(Type typeToEnsure) => serviceProvider.GetRequiredService(typeToEnsure).ShouldNotBeNull();

    private class ControllerDataSource : FindDerivedClassesDataSourceBase<ControllerBase, Startup> { }

    [Theory]
    [ClassData(typeof(ViewComponentsDataSource))]
    public void AutofacViewComponents(Type typeToEnsure) => serviceProvider.GetRequiredService(typeToEnsure).ShouldNotBeNull();

    public class ViewComponentsDataSource : FindDerivedClassesDataSourceBase<ViewComponent, Startup> { }

    [Theory]
    [ClassData(typeof(PageModelDataSource))]
    public void PageModels(Type typeToEnsure) => ActivatorUtilities.CreateInstance(serviceProvider, typeToEnsure).ShouldNotBeNull();

    public class PageModelDataSource : FindDerivedClassesDataSourceBase<PageModel, Startup> { }
}


