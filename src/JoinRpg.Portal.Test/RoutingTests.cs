using System.Reflection;
using JoinRpg.Portal.Controllers;
using JoinRpg.Portal.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test;

public class RoutingTests
{
    [SkippableTheory]
    [ClassData(typeof(ControllerDataSource))]

    public void GameControllersShouldHaveProjectIdInRoute(TypeInfo controllerType)
    {
        Skip.If(controllerType == typeof(GameController)); //This is special controller, we need to refactor it
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        _ = routeAttribute.ShouldNotBeNull();
        routeAttribute.Template.ShouldStartWith("{projectId}");
    }

    [SkippableTheory]
    [ClassData(typeof(LegacyControllerDataSource))]
    public void LegacyGameControllersShouldHaveProjectIdInRoute(TypeInfo controllerType)
    {
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        _ = routeAttribute.ShouldNotBeNull();
        routeAttribute.Template.ShouldStartWith("{projectId}");
    }

    [Theory]
    [InlineData(typeof(PlotListController), nameof(PlotListController.Index), "{projectId}/plots/Index")]
    [InlineData(typeof(PlotListController), nameof(PlotListController.InWork), "{projectId}/plots/InWork")]
    [InlineData(typeof(PlotListController), nameof(PlotListController.FlatList), "{projectId}/plots/FlatList")]
    [InlineData(typeof(PlotController), "CreateElement", "{projectId}/plots/CreateElement")]
    [InlineData(typeof(PlotController), "Edit", "{projectId}/plots/Edit")]
    public void ControllerActionResolvesToExpectedRoute(Type controllerType, string actionName, string expectedTemplate)
    {
        var classRoute = controllerType.GetCustomAttribute<RouteAttribute>()!.Template;
        var resolved = classRoute.Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);
        resolved.ShouldBe(expectedTemplate);
    }

    [Fact]
    public void PlotLegacyRedirectShouldCatchOldPrefix()
    {
        var route = typeof(PlotLegacyRedirectController).GetCustomAttribute<RouteAttribute>();
        _ = route.ShouldNotBeNull();
        route.Template.ShouldBe("{projectId}/plot");
    }

    [Obsolete]
    private class LegacyControllerDataSource : FindDerivedClassesDataSourceBase<ControllerGameBase, Startup> { }

    private class ControllerDataSource : FindDerivedClassesDataSourceBase<JoinControllerGameBase, Startup> { }
}
