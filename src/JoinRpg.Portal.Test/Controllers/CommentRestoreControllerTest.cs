using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.AspNetCore.Http.HttpResults;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>Восстановление комментариев (DRP, #5492): права проверены по проекту из запроса — заявка должна быть из него же.</summary>
public class CommentRestoreControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ProjectIdentification OtherProject = new(2);

    private readonly FakeClient client = new();

    private CommentRestoreController Controller => new(client);

    [Fact]
    public async Task Apply_ClaimOfOtherProject_Refused()
    {
        var result = await Controller.Apply(ProjectId, new ClaimIdentification(OtherProject, 9));

        _ = result.Result.ShouldBeOfType<BadRequest<string>>();
        client.Applied.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_OwnProject_Passed()
    {
        var claimId = new ClaimIdentification(ProjectId, 9);

        var result = await Controller.Apply(ProjectId, claimId);

        result.Result.ShouldBeOfType<Ok<CommentRestoreApplyResult>>().Value.ShouldBe(client.Result);
        client.Applied.ShouldBe([claimId]);
    }

    [Fact]
    public async Task Apply_ServiceRefused_BadRequestWithReason()
    {
        client.ApplyError = new JoinValidationException("Заявки нет в базе");

        var result = await Controller.Apply(ProjectId, new ClaimIdentification(ProjectId, 9));

        result.Result.ShouldBeOfType<BadRequest<string>>().Value.ShouldBe("Заявки нет в базе");
    }

    /// <summary>
    /// Блокировку проекта отдаёт WebApiExceptionHandler — русским текстом с 409, а не английским Message в 400 (#5735).
    /// </summary>
    [Fact]
    public async Task Apply_BlockedProject_GoesToCommonHandler()
    {
        client.ApplyError = new ProjectBlockedException(ProjectId);

        _ = await Should.ThrowAsync<ProjectBlockedException>(Controller.Apply(ProjectId, new ClaimIdentification(ProjectId, 9)));
    }

    private sealed class FakeClient : ICommentRestoreClient
    {
        public List<ClaimIdentification> Applied { get; } = [];

        public CommentRestoreApplyResult Result { get; } = new(1, 0, []);

        public Exception? ApplyError { get; set; }

        public Task<CommentRestorePlanViewModel> GetPlan(ProjectIdentification projectId) => throw new NotSupportedException();

        public Task<CommentRestoreApplyResult> Apply(ClaimIdentification claimId)
        {
            Applied.Add(claimId);
            return ApplyError is { } error ? throw error : Task.FromResult(Result);
        }
    }
}
