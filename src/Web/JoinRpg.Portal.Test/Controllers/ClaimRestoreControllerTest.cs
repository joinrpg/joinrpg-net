using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.AspNetCore.Http.HttpResults;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>Восстановление проекта (DRP, #5529): объекты чужого проекта сервис не видит.</summary>
public class ClaimRestoreControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ProjectIdentification OtherProject = new(2);

    private readonly FakeClient client = new();

    private ClaimRestoreController Controller => new(client);

    [Fact]
    public async Task ApplyCharacter_GroupOfOtherProject_Refused()
    {
        var result = await Controller.ApplyCharacter(ProjectId, new CharacterRestoreApplyRequest(
            new CharacterIdentification(ProjectId, 5), Create: false, Name: null,
            [new CharacterGroupIdentification(OtherProject, 7)], []));

        _ = result.Result.ShouldBeOfType<BadRequest<string>>();
        client.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyCharacter_FieldOfOtherProject_Refused()
    {
        var result = await Controller.ApplyCharacter(ProjectId, new CharacterRestoreApplyRequest(
            new CharacterIdentification(ProjectId, 5), Create: false, Name: null, null,
            [new RestoreFieldValueRequest(new ProjectFieldIdentification(OtherProject, 3), "x")]));

        _ = result.Result.ShouldBeOfType<BadRequest<string>>();
        client.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyClaimFields_FieldOfOtherProject_Refused()
    {
        var result = await Controller.ApplyClaimFields(ProjectId, new ClaimFieldsRestoreApplyRequest(
            new ClaimIdentification(ProjectId, 9),
            [new RestoreFieldValueRequest(new ProjectFieldIdentification(OtherProject, 3), "x")]));

        _ = result.Result.ShouldBeOfType<BadRequest<string>>();
        client.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyClaimFields_OwnProject_Passed()
    {
        var result = await Controller.ApplyClaimFields(ProjectId, new ClaimFieldsRestoreApplyRequest(
            new ClaimIdentification(ProjectId, 9),
            [new RestoreFieldValueRequest(new ProjectFieldIdentification(ProjectId, 3), "x")]));

        _ = result.Result.ShouldBeOfType<Ok>();
        client.Calls.ShouldBe(["ApplyClaimFields"]);
    }

    [Fact]
    public async Task GetPlan_BrokenPayload_BadRequest()
    {
        client.PlanError = new FormatException("Invalid master export payload");

        var result = await Controller.GetPlan(ProjectId, new ClaimRestorePlanRequest(ProjectId, [new MasterExportForPlan("x", DateTimeOffset.Now)]));

        result.Result.ShouldBeOfType<BadRequest<string>>().Value.ShouldNotBeNull().ShouldContain("загрузите файл заново");
    }

    /// <summary>
    /// Блокировку проекта отдаёт WebApiExceptionHandler — русским текстом с 409, а не английским Message в 400 (#5735).
    /// </summary>
    [Fact]
    public async Task ApplyClaimFields_BlockedProject_GoesToCommonHandler()
    {
        client.ApplyError = new ProjectBlockedException(ProjectId);

        _ = await Should.ThrowAsync<ProjectBlockedException>(Controller.ApplyClaimFields(ProjectId, new ClaimFieldsRestoreApplyRequest(
            new ClaimIdentification(ProjectId, 9),
            [new RestoreFieldValueRequest(new ProjectFieldIdentification(ProjectId, 3), "x")])));
    }

    private sealed class FakeClient : IClaimRestoreClient
    {
        public List<string> Calls { get; } = [];

        public Exception? PlanError { get; set; }

        public Task<ClaimRestorePlanViewModel> GetPlan(ClaimRestorePlanRequest request)
            => PlanError is { } error ? throw error : throw new NotSupportedException();

        public Task<MasterExportUploadResult> ParseExport(ProjectIdentification projectId, string fileName, Stream content) => throw new NotSupportedException();

        public Task<ClaimRestoreApplyResult> Apply(ClaimRestoreApplyRequest request) => throw new NotSupportedException();

        public Task ApplyGroup(GroupRestoreApplyRequest request) => Record(nameof(ApplyGroup));

        public Task ApplyCharacter(CharacterRestoreApplyRequest request) => Record(nameof(ApplyCharacter));

        public Task ApplyClaimFields(ClaimFieldsRestoreApplyRequest request) => Record(nameof(ApplyClaimFields));

        public Exception? ApplyError { get; set; }

        private Task Record(string call)
        {
            Calls.Add(call);
            return ApplyError is { } error ? throw error : Task.CompletedTask;
        }
    }
}
