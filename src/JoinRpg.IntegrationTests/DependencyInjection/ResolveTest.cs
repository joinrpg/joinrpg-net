using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Portal;
using JoinRpg.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.IntegrationTests.DependencyInjection;

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

    private class ControllerDataSource : FindDerivedClassesDataSourceBase<Controller, Startup> { }

    [Theory]
    [ClassData(typeof(ViewComponentsDataSource))]
    public void AutofacViewComponents(Type typeToEnsure) => serviceProvider.GetRequiredService(typeToEnsure).ShouldNotBeNull();

    public class ViewComponentsDataSource : FindDerivedClassesDataSourceBase<ViewComponent, Startup> { }
}


