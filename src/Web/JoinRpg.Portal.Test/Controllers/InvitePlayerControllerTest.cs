using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Серверная проверка приглашения не должна зависеть от того, что остров корректно себя ведёт (#5292).
/// </summary>
public class InvitePlayerControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1611);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveCharacterIdIsRejected(int characterId)
    {
        var client = new FakeClient();
        var controller = new InvitePlayerController(client);

        var result = await controller.Invite(ProjectId, new CharacterIdentification(ProjectId, characterId), "42", "Приглашение");

        _ = result.Result.ShouldBeOfType<BadRequest<string>>();
        client.Called.ShouldBeFalse();
    }

    [Fact]
    public async Task SelectedCharacterIsInvited()
    {
        var client = new FakeClient();
        var controller = new InvitePlayerController(client);

        var result = await controller.Invite(ProjectId, new CharacterIdentification(ProjectId, 124501), "42", "Приглашение");

        _ = result.Result.ShouldBeOfType<Ok<ClaimIdentification>>();
        client.Called.ShouldBeTrue();
    }

    /// <summary>
    /// Блокировку проекта отдаёт WebApiExceptionHandler — русским текстом с 409, а не английским Message в 400 (#5735).
    /// </summary>
    [Fact]
    public async Task BlockedProjectGoesToCommonHandler()
    {
        var controller = new InvitePlayerController(new FakeClient { Failure = new ProjectBlockedException(ProjectId) });

        _ = await Should.ThrowAsync<ProjectBlockedException>(
            controller.Invite(ProjectId, new CharacterIdentification(ProjectId, 124501), "42", "Приглашение"));
    }

    private sealed class FakeClient : IInvitePlayerClient
    {
        public bool Called { get; private set; }
        public Exception? Failure { get; init; }

        public Task<ClaimIdentification> InvitePlayer(CharacterIdentification CharacterId, string UserLink, string ClaimText)
        {
            Called = true;
            return Failure is null
                ? Task.FromResult(new ClaimIdentification(CharacterId.ProjectId, 1))
                : Task.FromException<ClaimIdentification>(Failure);
        }
    }
}
