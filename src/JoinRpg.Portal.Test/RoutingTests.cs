using System.Reflection;
using JoinRpg.Portal.Controllers;
using JoinRpg.Portal.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test;

public class RoutingTests
{
    [Theory]
    [ClassData(typeof(ControllerDataSource))]

    public void GameControllersShouldHaveProjectIdInRoute(TypeInfo controllerType)
    {
        if (controllerType == typeof(GameController))
        {
            //This is special controller, we need to refactor it
            return;
        }
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        _ = routeAttribute.ShouldNotBeNull();
        routeAttribute.Template.ShouldStartWith("{projectId}");
    }

    private class ControllerDataSource : FindDerivedClassesDataSourceBase<ControllerGameBase, Startup> { }
}
