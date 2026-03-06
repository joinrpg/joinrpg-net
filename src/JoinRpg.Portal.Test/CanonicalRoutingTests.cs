using JoinRpg.Portal.Controllers;
using JoinRpg.Portal.Controllers.Money;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

/// <summary>
/// Это класс проверяет, что канонические пути во что-то резолвятся. Это помогает не ломать внешние ссылки.
/// Если ты поменял реализацию страницы и перенес ее куда-то, то поменяй тут.
/// Если страница исчезла, сделай редирект куда-то в разумное место.
/// </summary>
public class CanonicalRoutingTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
{
    private readonly IServiceProvider _services = factory.Services;

    [Theory]
    [InlineData("{projectId}/plot/{**path}", typeof(PlotLegacyRedirectController), nameof(PlotLegacyRedirectController.LegacyRedirect))]
    [InlineData("{projectId}/plots/edit", typeof(PlotController), nameof(PlotController.Edit))]
    [InlineData("{projectId}/plots", typeof(PlotListController), nameof(PlotListController.Index))]
    [InlineData("{projectId}/plots/index", typeof(PlotListController), nameof(PlotListController.Index))]
    [InlineData("{projectId}/plots/createelement", typeof(PlotController), nameof(PlotController.CreateElement))]
    [InlineData("{projectId}/plots/inwork", typeof(PlotListController), nameof(PlotListController.InWork))]
    [InlineData("{projectId}/plots/flatlist", typeof(PlotListController), nameof(PlotListController.FlatList))]
    [InlineData("{projectId}/plots/ready", typeof(PlotListController), nameof(PlotListController.Ready))]
    [InlineData("{projectId}/rooms/addroomtype", typeof(AccommodationTypeController), nameof(AccommodationTypeController.AddRoomType))]
    [InlineData("{projectId}/character/{characterid}/details", typeof(CharacterController), nameof(CharacterController.Details))]
    [InlineData("{projectId}/characters/active", typeof(CharacterListController), nameof(CharacterListController.Active))]
    [InlineData("{projectId}/checkin/setup", typeof(CheckInController), nameof(CheckInController.Setup))]
    [InlineData("{ProjectId}/claim/{ClaimId}/edit", typeof(ClaimController), nameof(ClaimController.Edit))]
    [InlineData("my/claims", typeof(MyClaimListController), nameof(MyClaimListController.My))]
    [InlineData("{projectId}/money/setup", typeof(FinancesController), nameof(FinancesController.Setup))]
    [InlineData("{projectId}/forums/{forumThreadId}/viewthread", typeof(ForumController), nameof(ForumController.ViewThread))]
    [InlineData("{ProjectId}/fields/create", typeof(GameFieldController), nameof(GameFieldController.Create))]
    [InlineData("{projectId}/roles/{characterGroupId}/edit", typeof(GameGroupsController), nameof(GameGroupsController.Edit))]
    [InlineData("{projectId}/subscribe/bymaster/{masterId}", typeof(GameSubscribeController), nameof(GameSubscribeController.ByMaster))]
    [InlineData("{projectId}/tools/apis", typeof(GameToolsController), nameof(GameToolsController.Apis))]
    [InlineData("{projectId}/massmail/forclaims", typeof(MassMailController), nameof(MassMailController.ForClaims))]
    [InlineData("{projectId}/print/character", typeof(PrintController), nameof(PrintController.Character))]
    [InlineData("{projectId}/money/transfer/create", typeof(TransferController), nameof(TransferController.Create))]
    public void CanonicalRouteResolvesToAction(
        string canonicalRoute, Type expectedController, string expectedAction)
    {
        var endpointSource = _services.GetRequiredService<EndpointDataSource>();

        var endpoints = endpointSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(ep => (ep, descriptor: ep.Metadata.GetMetadata<ControllerActionDescriptor>()))
            .Where(x => x.descriptor is not null)
            .Select(x => (x.ep.RoutePattern, ControllerType: x.descriptor!.ControllerTypeInfo.AsType(), x.descriptor.ActionName))
            .ToList();

        var expectedEndpoints = endpoints
            .Where(x => x.ControllerType == expectedController && x.ActionName == expectedAction)
            .ToList();

        var actualEndpoint = endpoints
            .Where(x => x.RoutePattern.RawText?.Equals(
                    canonicalRoute, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        actualEndpoint.ShouldNotBeEmpty($"Маршрут '{canonicalRoute}' не зарегистрирован");
        actualEndpoint.ShouldBeSubsetOf(expectedEndpoints);
    }
}
