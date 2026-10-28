using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Общие правила единого входа обсуждения (<see cref="ICommentDiscussionService"/>, ADR026 §5) на
/// живом контейнере: сервис собирается из DI целиком, вместе с операциями заявки и форума.
/// </summary>
/// <remarks>
/// Форма комментария на ошибке молча редиректит, поэтому отказ проверяется на сервисе, а не через
/// ручку. Ручку острова (<c>webapi/claim-comment</c>) проверяет <see cref="FinanceModerationApiScenario"/>.
/// </remarks>
public class CommentDiscussionServiceScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Раньше форум молча делал такой ответ скрытым; теперь — отказ, как у заявки.
    /// </summary>
    [Fact]
    public async Task VisibleReplyToHiddenComment_InThread_IsRejected()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для ответа на скрытый");
        }

        var threadId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IForumService>().CreateThread(
                projectInfo.GroupTree.RootGroupId,
                "Тема со скрытым комментарием",
                "Первый комментарий в теме",
                hideFromUser: false,
                emailEverybody: false);
        });
        await factory.Services.RunAsAsync(masterId, sp =>
            sp.AddThreadComment(threadId, parentCommentId: null, isVisibleToPlayer: false, "Только мастерам"));
        var hiddenId = GetCommentId(threadId, "Только мастерам").ShouldNotBeNull();

        _ = await Should.ThrowAsync<VisibleReplyToHiddenCommentException>(() =>
            factory.Services.RunAsAsync(masterId, sp =>
                sp.AddThreadComment(threadId, hiddenId, isVisibleToPlayer: true, "Видимый ответ")));
        GetCommentId(threadId, "Видимый ответ").ShouldBeNull();

        // Скрытый ответ на скрытый — можно.
        await factory.Services.RunAsAsync(masterId, sp =>
            sp.AddThreadComment(threadId, hiddenId, isVisibleToPlayer: false, "Скрытый ответ"));
        _ = GetCommentId(threadId, "Скрытый ответ").ShouldNotBeNull();
    }

    /// <summary>
    /// Раньше сервис молча делал комментарий игрока видимым, а отказ давали только вызывающие.
    /// </summary>
    [Fact]
    public async Task PlayerAsksToHideComment_InClaim_IsRejected()
    {
        UserIdentification masterId;
        UserIdentification playerId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для скрытия игроком");
        }

        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });
        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        _ = await Should.ThrowAsync<NoAccessToProjectException>(() =>
            factory.Services.RunAsAsync(playerId, sp =>
                sp.AddClaimComment(claimId, parentCommentId: null, isVisibleToPlayer: false, "Спрячьте от меня")));
        CommentExists(claimId, "Спрячьте от меня").ShouldBeFalse();

        await factory.Services.RunAsAsync(playerId, sp =>
            sp.AddClaimComment(claimId, parentCommentId: null, isVisibleToPlayer: true, "Обычный комментарий"));
        CommentExists(claimId, "Обычный комментарий").ShouldBeTrue();
    }

    private CommentId? GetCommentId(ForumThreadIdentification threadId, string text)
    {
        using var scope = factory.Services.CreateScope();
        var id = scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<ForumThread>()
            .Where(t => t.ForumThreadId == threadId.ThreadId)
            .SelectMany(t => t.CommentDiscussion.Comments)
            .Where(c => c.CommentText.Text.Contents == text)
            .Select(c => (int?)c.CommentId)
            .SingleOrDefault();
        return id is int value ? new CommentId(value) : null;
    }

    private bool CommentExists(ClaimIdentification claimId, string text)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<Claim>()
            .Where(c => c.ClaimId == claimId.ClaimId)
            .SelectMany(c => c.CommentDiscussion.Comments)
            .Any(c => c.CommentText.Text.Contents == text);
    }
}
