using System.Reflection;
using JoinRpg.Portal.Controllers;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Controllers.Money;
using JoinRpg.Portal.Controllers.Schedule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

public class RoutingTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
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

    /// <summary>
    /// Ожидания здесь — документация роутов: они сверяются с настоящей таблицей эндпойнтов
    /// ASP.NET, а не собираются из шаблона класса (так тест годами не замечал шаблоны на самих
    /// методах, см. #5205). Если у action'а несколько роутов, перечисляй их все: сверка идёт
    /// по полному набору, чтобы новый или потерянный роут было видно.
    /// </summary>
    [Theory]
    [InlineData(typeof(AccommodationPrintController), nameof(AccommodationPrintController.MainReport), "{projectId}/rooms/report")]
    [InlineData(typeof(AccommodationTypeController), nameof(AccommodationTypeController.AddRoomType), "{projectId}/rooms/AddRoomType")]
    [InlineData(typeof(AclController), nameof(AclController.Add), "{projectId}/masters/add/{userId}")]
    [InlineData(typeof(CharacterController), nameof(CharacterController.Details), "{projectId}/character/{characterid}", "{projectId}/character/{characterid}/details")]
    [InlineData(typeof(CharacterListController), nameof(CharacterListController.Active), "{projectId}/characters/Active")]
    [InlineData(typeof(CheckInController), nameof(CheckInController.Setup), "{projectId}/checkin/Setup")]
    [InlineData(typeof(ClaimController), nameof(ClaimController.Edit), "{ProjectId}/claim/{ClaimId}/Edit")]
    [InlineData(typeof(DiscussionRedirectController), nameof(DiscussionRedirectController.ToDiscussion), "{ProjectId}/goto/discussion/{CommentDiscussionId}")]
    [InlineData(typeof(FinancesController), nameof(FinancesController.Setup), "{projectId}/money/Setup")]
    [InlineData(typeof(ForumController), nameof(ForumController.ViewThread), "{projectId}/forums/{forumThreadId}/ViewThread")]
    [InlineData(typeof(GameFieldController), nameof(GameFieldController.Create), "{ProjectId}/fields/Create")]
    [InlineData(typeof(GameGroupsController), nameof(GameGroupsController.Edit), "{projectId}/roles/{characterGroupId}/Edit")]
    [InlineData(typeof(GameSubscribeController), nameof(GameSubscribeController.ByMaster), "{projectId}/subscribe/ByMaster/{masterId}")]
    [InlineData(typeof(GameToolsController), nameof(GameToolsController.Apis), "{projectId}/tools/Apis")]
    [InlineData(typeof(MassMailController), nameof(MassMailController.ForClaims), "{projectId}/massmail/ForClaims")]
    [InlineData(typeof(PlotListController), nameof(PlotListController.InWork), "{projectId}/plots/InWork")]
    [InlineData(typeof(PlotListController), nameof(PlotListController.FlatList), "{projectId}/plots/FlatList")]
    [InlineData(typeof(PlotListController), nameof(PlotListController.Ready), "{projectId}/plots/Ready")]
    [InlineData(typeof(PlotController), nameof(PlotController.CreateElement), "{projectId}/plots/CreateElement")]
    [InlineData(typeof(PlotController), nameof(PlotController.Edit), "{projectId}/plots/Edit")]
    [InlineData(typeof(PrintController), nameof(PrintController.Character), "{projectId}/print/Character")]
    [InlineData(typeof(PrintController), nameof(PrintController.HandoutOnly), "{projectId}/print/HandoutOnly")]
    [InlineData(typeof(ReportsController), nameof(ReportsController.Report2D), "{projectId}/reports/2d/{gameReport2DTemplateId}")]
    [InlineData(typeof(ShowScheduleController), nameof(ShowScheduleController.Ical), "{projectId}/schedule/ical")]
    [InlineData(typeof(TransferController), nameof(TransferController.Create), "{projectId}/money/transfer/Create")]
    public void ControllerActionResolvesToExpectedRoute(Type controllerType, string actionName, params string[] expectedTemplates)
    {
        var actualTemplates = RouteTemplatesFor(controllerType, actionName);

        actualTemplates.ShouldNotBeEmpty(
            $"У {controllerType.Name}.{actionName} нет ни одного зарегистрированного роута");
        Normalize(actualTemplates).ShouldBe(Normalize(expectedTemplates));

        static IEnumerable<string> Normalize(IEnumerable<string> templates)
            => templates.Select(t => t.ToLowerInvariant()).Order();
    }

    private List<string> RouteTemplatesFor(Type controllerType, string actionName)
        => factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(ep => (ep.RoutePattern.RawText, descriptor: ep.Metadata.GetMetadata<ControllerActionDescriptor>()))
            .Where(x => x.RawText is not null
                && x.descriptor is not null
                && x.descriptor.ControllerTypeInfo.AsType() == controllerType
                && x.descriptor.ActionName == actionName)
            .Select(x => x.RawText!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    [Fact]
    public void PlotLegacyRedirectShouldCatchOldPrefix()
    {
        var route = typeof(PlotLegacyRedirectController).GetCustomAttribute<RouteAttribute>();
        _ = route.ShouldNotBeNull();
        route.Template.ShouldBe("{projectId}/plot");
    }

    private class ControllerDataSource : FindDerivedClassesDataSourceBase<JoinControllerGameBase, Startup> { }
}
