using System.Reflection;
using JoinRpg.Portal.Controllers;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Controllers.Money;
using JoinRpg.Portal.Controllers.Schedule;
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

    [Theory]
    [InlineData(typeof(AccommodationPrintController), nameof(AccommodationPrintController.MainReport), "{projectId}/rooms/report/")]
    [InlineData(typeof(AccommodationTypeController), nameof(AccommodationTypeController.AddRoomType), "{projectId}/rooms/AddRoomType")]
    [InlineData(typeof(AclController), nameof(AclController.Add), "{projectId}/masters")]
    [InlineData(typeof(CharacterController), nameof(CharacterController.Details), "{projectId}/character/{characterid}/Details")]
    [InlineData(typeof(CharacterListController), nameof(CharacterListController.Active), "{projectId}/characters/Active")]
    [InlineData(typeof(CheckInController), nameof(CheckInController.Setup), "{projectId}/checkin/Setup")]
    [InlineData(typeof(ClaimController), nameof(ClaimController.Edit), "{ProjectId}/claim/{ClaimId}/edit")]
    [InlineData(typeof(DiscussionRedirectController), nameof(DiscussionRedirectController.ToDiscussion), "{ProjectId}/goto/")]
    [InlineData(typeof(FinancesController), nameof(FinancesController.Setup), "{projectId}/money/Setup")]
    [InlineData(typeof(ForumController), nameof(ForumController.ViewThread), "{projectId}/forums/{forumThreadId}/ViewThread")]
    [InlineData(typeof(GameFieldController), nameof(GameFieldController.Create), "{ProjectId}/fields/Create")]
    [InlineData(typeof(GameGroupsController), nameof(GameGroupsController.Edit), "{projectId}/roles/{characterGroupId}/Edit")]
    [InlineData(typeof(GameSubscribeController), nameof(GameSubscribeController.ByMaster), "{projectId}/subscribe/ByMaster")]
    [InlineData(typeof(GameToolsController), nameof(GameToolsController.Apis), "{projectId}/tools/Apis")]
    [InlineData(typeof(MassMailController), nameof(MassMailController.ForClaims), "{projectId}/massmail/ForClaims")]
    [InlineData(typeof(PlotListController), nameof(PlotListController.InWork), "{projectId}/plots/InWork")]
    [InlineData(typeof(PlotListController), nameof(PlotListController.FlatList), "{projectId}/plots/FlatList")]
    [InlineData(typeof(PlotListController), nameof(PlotListController.Ready), "{projectId}/plots/Ready")]
    [InlineData(typeof(PlotController), nameof(PlotController.CreateElement), "{projectId}/plots/CreateElement")]
    [InlineData(typeof(PlotController), nameof(PlotController.Edit), "{projectId}/plots/Edit")]
    [InlineData(typeof(PrintController), nameof(PrintController.Character), "{projectId}/print/Character")]
    [InlineData(typeof(ReportsController), nameof(ReportsController.Report2D), "{projectId}/reports")]
    [InlineData(typeof(ShowScheduleController), nameof(ShowScheduleController.Ical), "{projectId}/schedule")]
    [InlineData(typeof(TransferController), nameof(TransferController.Create), "{projectId}/money/transfer/Create")]
    public void ControllerActionResolvesToExpectedRoute(Type controllerType, string actionName, string expectedTemplate)
    {
        var classRoute = controllerType.GetCustomAttribute<RouteAttribute>()!.Template;
        var resolved = classRoute.Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);
        resolved.ShouldBe(expectedTemplate, StringCompareShould.IgnoreCase);
    }

    [Fact]
    public void PlotLegacyRedirectShouldCatchOldPrefix()
    {
        var route = typeof(PlotLegacyRedirectController).GetCustomAttribute<RouteAttribute>();
        _ = route.ShouldNotBeNull();
        route.Template.ShouldBe("{projectId}/plot");
    }

    private class ControllerDataSource : FindDerivedClassesDataSourceBase<JoinControllerGameBase, Startup> { }
}
