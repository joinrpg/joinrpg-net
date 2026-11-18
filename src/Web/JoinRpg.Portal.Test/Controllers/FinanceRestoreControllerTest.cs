using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.AspNetCore.Http.HttpResults;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>Восстановление ручных оплат (DRP, #5631): права проверены по проекту из запроса — заявка должна быть из него же.</summary>
public class FinanceRestoreControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ProjectIdentification OtherProject = new(2);

    private readonly FakeClient client = new();

    private FinanceRestoreController Controller => new(client);

    private static FinanceRestoreApplyRequest Request(ProjectIdentification projectId)
        => new(new ClaimIdentification(projectId, 9), new Dictionary<string, int> { ["id:100"] = 10 });

    [Fact]
    public async Task Apply_ClaimOfOtherProject_Refused()
    {
        var result = await Controller.Apply(ProjectId, Request(OtherProject));

        _ = result.Result.ShouldBeOfType<BadRequest<string>>();
        client.Applied.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_OwnProject_Passed()
    {
        var request = Request(ProjectId);

        var result = await Controller.Apply(ProjectId, request);

        result.Result.ShouldBeOfType<Ok<FinanceRestoreApplyResult>>().Value.ShouldBe(client.Result);
        client.Applied.ShouldBe([request]);
    }

    [Fact]
    public async Task Apply_ServiceRefused_BadRequestWithReason()
    {
        client.Error = new JoinValidationException("Заявки нет в базе");

        var result = await Controller.Apply(ProjectId, Request(ProjectId));

        result.Result.ShouldBeOfType<BadRequest<string>>().Value.ShouldBe("Заявки нет в базе");
    }

    [Fact]
    public async Task CreateOwnCash_ServiceRefused_BadRequestWithReason()
    {
        client.Error = new JoinValidationException("Проект не заблокирован");

        var result = await Controller.CreateOwnCash(ProjectId);

        result.Result.ShouldBeOfType<BadRequest<string>>().Value.ShouldBe("Проект не заблокирован");
    }

    /// <summary>
    /// Блокировку проекта и архив отдаёт WebApiExceptionHandler — русским текстом с 409, а не английским Message в 400 (#5735).
    /// </summary>
    [Fact]
    public async Task Apply_BlockedProject_GoesToCommonHandler()
    {
        client.Error = new ProjectBlockedException(ProjectId);

        _ = await Should.ThrowAsync<ProjectBlockedException>(Controller.Apply(ProjectId, Request(ProjectId)));
    }

    [Fact]
    public async Task CreateOwnCash_ArchivedProject_GoesToCommonHandler()
    {
        client.Error = new ProjectDeactivatedException(ProjectId);

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(Controller.CreateOwnCash(ProjectId));
    }

    private sealed class FakeClient : IFinanceRestoreClient
    {
        public List<FinanceRestoreApplyRequest> Applied { get; } = [];

        public FinanceRestoreApplyResult Result { get; } = new(1, 0, 0, 0, []);

        public Exception? Error { get; set; }

        public Task<FinanceRestorePlanViewModel> GetPlan(ProjectIdentification projectId) => throw new NotSupportedException();

        public Task<FinanceRestoreApplyResult> Apply(FinanceRestoreApplyRequest request)
        {
            Applied.Add(request);
            return Error is { } error ? throw error : Task.FromResult(Result);
        }

        public Task CreateOwnCash(ProjectIdentification projectId) => Error is { } error ? throw error : Task.CompletedTask;
    }
}
