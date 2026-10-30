using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.Portal.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Старые списки заявок переехали на грид <c>/{projectId}/claims</c> (ADR027 §6): экшены стали
/// 301-редиректами, и адрес цели фиксируется кешированием 301 браузером. Каждый маршрут из
/// таблицы §6 проверен на точный адрес назначения; параметры выгрузки и страницы пробрасываются.
/// </summary>
public class ClaimListRedirectTests
{
    private static readonly ProjectIdentification ProjectId = new(1620);

    /// <summary>
    /// Редиректные экшены не ходят в базу и в зависимости контроллера не заглядывают, поэтому
    /// зависимости не нужны. HttpContext нужен для проброса query-параметров.
    /// </summary>
    private static ClaimListController CreateController(string? query = null)
        => new(null!, null!, null!, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Request = { QueryString = new QueryString(query) },
                },
            },
        };

    private static string GroupQuery(CharacterGroupIdentification groupId)
        => Uri.EscapeDataString(groupId.ToString());

    private static string MasterQuery(UserIdentification masterId)
        => Uri.EscapeDataString(masterId.ToString());

    private static void ShouldRedirectToGrid(ActionResult result, string expectedUrl)
    {
        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Permanent.ShouldBeTrue();
        redirect.Url.ShouldBe(expectedUrl);
    }

    [Fact]
    public void ActiveList_RedirectsToGrid()
        => ShouldRedirectToGrid(CreateController().ActiveList(ProjectId, export: null), "/1620/claims");

    [Fact]
    public void Discussing_RedirectsToDiscussingStatus()
        => ShouldRedirectToGrid(
            CreateController().Discussing(ProjectId, export: null),
            "/1620/claims?status=Discussion");

    [Fact]
    public void OnHoldList_RedirectsToOnHoldStatus()
        => ShouldRedirectToGrid(
            CreateController().OnHoldList(ProjectId, export: null),
            "/1620/claims?status=OnHold");

    /// <summary>
    /// Старый список брал заявки любых статусов — статус «Все» покрывает проблемы отклонённых
    /// заявок и удалённых персонажей (ADR027 §6).
    /// </summary>
    [Fact]
    public void Problems_RedirectsToAllStatusWithProblemsFilter()
        => ShouldRedirectToGrid(
            CreateController().Problems(ProjectId, export: null),
            "/1620/claims?status=All&onlyWithProblems=true");

    [Fact]
    public void ListForGroup_RedirectsToGroupFilter()
    {
        var groupId = new CharacterGroupIdentification(ProjectId, 43014);

        ShouldRedirectToGrid(
            CreateController().ListForGroup(groupId, export: null),
            $"/1620/claims?characterGroupId={GroupQuery(groupId)}");
    }

    [Fact]
    public void DiscussingForGroup_RedirectsToGroupAndDiscussingStatus()
    {
        var groupId = new CharacterGroupIdentification(ProjectId, 43014);

        ShouldRedirectToGrid(
            CreateController().DiscussingForGroup(groupId, export: null),
            $"/1620/claims?status=Discussion&characterGroupId={GroupQuery(groupId)}");
    }

    [Fact]
    public void Responsible_RedirectsToResponsibleFilter()
    {
        var masterId = new UserIdentification(51);

        ShouldRedirectToGrid(
            CreateController().Responsible(ProjectId, masterId, export: null),
            $"/1620/claims?responsibleMasterId={MasterQuery(masterId)}");
    }

    [Fact]
    public void ResponsibleDiscussing_RedirectsToResponsibleAndDiscussingStatus()
    {
        var masterId = new UserIdentification(51);

        ShouldRedirectToGrid(
            CreateController().ResponsibleDiscussing(ProjectId, masterId, export: null),
            $"/1620/claims?status=Discussion&responsibleMasterId={MasterQuery(masterId)}");
    }

    [Fact]
    public void ResponsibleOnHold_RedirectsToResponsibleAndOnHoldStatus()
    {
        var masterId = new UserIdentification(51);

        ShouldRedirectToGrid(
            CreateController().ResponsibleOnHold(ProjectId, masterId, export: null),
            $"/1620/claims?status=OnHold&responsibleMasterId={MasterQuery(masterId)}");
    }

    [Fact]
    public void ResponsibleProblems_RedirectsToResponsibleAllStatusAndProblemsFilter()
    {
        var masterId = new UserIdentification(51);

        ShouldRedirectToGrid(
            CreateController().ResponsibleProblems(ProjectId, masterId, export: null),
            $"/1620/claims?status=All&onlyWithProblems=true&responsibleMasterId={MasterQuery(masterId)}");
    }

    /// <summary>На выгрузки старых списков бывают закладки — редирект пробрасывает export и page.</summary>
    [Fact]
    public void Redirect_PassesThroughExportAndPageFromQuery()
    {
        var controller = CreateController("?page=3&export=csv");

        ShouldRedirectToGrid(
            controller.Problems(ProjectId, export: "csv"),
            "/1620/claims?status=All&page=3&onlyWithProblems=true&export=csv");
    }

    [Fact]
    public void Redirect_IgnoresGarbagePage()
        => ShouldRedirectToGrid(
            CreateController("?page=abc").ActiveList(ProjectId, export: null),
            "/1620/claims");
}
