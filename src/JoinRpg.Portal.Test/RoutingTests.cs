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

    [Obsolete]
    private class LegacyControllerDataSource : FindDerivedClassesDataSourceBase<ControllerGameBase, Startup> { }

    private class ControllerDataSource : FindDerivedClassesDataSourceBase<JoinControllerGameBase, Startup> { }
}
