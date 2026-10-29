using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Счётчик непрочитанного в SQL (<c>CommentDiscussionPredicates.UnreadCount</c>) совпадает с правилом
/// в памяти — <see cref="ExpectedUnread"/> — на живой базе (ADR026, §2).
/// </summary>
/// <remarks>
/// В обсуждение кладутся свои комментарии, чужие скрытые от игроков, отметки прочтения с дублями и
/// с отметкой ниже максимума. Числа сверяются для мастера, игрока и пользователя без отметок —
/// в списке тем форума и в списке заявок.
/// </remarks>
public class UnreadCommentsCountScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task UnreadCount_InSql_MatchesInMemoryRule()
    {
        UserIdentification masterId, playerId, otherId, readerId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            otherId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            readerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для счётчика непрочитанного");
        }

        var (threadId, characterId) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            var threadId = await sp.GetRequiredService<IForumService>().CreateThread(
                projectInfo.GroupTree.RootGroupId,
                "Тема для счётчика",
                "Первый комментарий мастера",
                hideFromUser: false,
                emailEverybody: false);

            var characterId = await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
            return (threadId, characterId);
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        var threadDiscussionId = GetThreadDiscussionId(threadId);
        var claimDiscussionId = GetClaimDiscussionId(claimId);

        SeedDiscussion(projectId, threadDiscussionId, masterId, playerId, otherId);
        SeedDiscussion(projectId, claimDiscussionId, masterId, playerId, otherId);

        var users = new[] { masterId, playerId, readerId };

        foreach (var userId in users)
        {
            var (projectInfo, threads, unreadClaims) = await factory.Services.RunAsAsync(userId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
                var isMaster = projectInfo.HasMasterAccess(userId);
                var threads = await sp.GetRequiredService<IForumRepository>().GetThreads(projectId.Value, userId, isMaster, groupIds: null);
                var unreadClaims = await sp.GetRequiredService<IClaimsRepository>().GetUnreadDiscussionsForClaims(
                    projectId.Value, ClaimStatusSpec.Any, userId.Value, isMaster);
                return (projectInfo, threads, unreadClaims);
            });

            var thread = threads.ShouldHaveSingleItem();
            var threadDiscussion = LoadDiscussion(threadDiscussionId);
            var isMaster = projectInfo.HasMasterAccess(userId);
            thread.UnreadCount.ShouldBe(ExpectedUnread(threadDiscussion, userId, isMaster), $"тема, пользователь {userId}");
            thread.TotalCount.ShouldBe(
                threadDiscussion.Comments.Count(c => c.IsVisibleToPlayer || isMaster), $"тема, всего, пользователь {userId}");

            var claimDiscussion = LoadDiscussion(claimDiscussionId);
            unreadClaims[new CommentDiscussionId(claimDiscussionId)]
                .ShouldBe(ExpectedUnread(claimDiscussion, userId, isMaster), $"заявка, пользователь {userId}");
        }

        // Числа руками по теме: первый комментарий мастера (c0), затем SeedDiscussion (c1–c8).
        // Мастер: отметка — max(свои c0, c2, c5; отметки c1, c1) = c5, выше — c6, c7, c8.
        // Игрок: max(свой c3; отметки c4, c4, c2) = c4, выше видимый только c7.
        // Без отметок и комментариев — все видимые игроку: c0, c1, c3, c4, c7.
        // Обсуждение заявки засеяно позже, его id и отметки выше: если бы выражение брало отметки
        // чужого обсуждения, у мастера и игрока здесь были бы нули.
        var expected = new Dictionary<UserIdentification, int> { [masterId] = 3, [playerId] = 1, [readerId] = 5 };
        foreach (var (userId, count) in expected)
        {
            var thread = await factory.Services.RunAsAsync(userId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
                var threads = await sp.GetRequiredService<IForumRepository>().GetThreads(
                    projectId.Value, userId, projectInfo.HasMasterAccess(userId), groupIds: null);
                return threads.Single();
            });
            thread.UnreadCount.ShouldBe(count, $"пользователь {userId}");
        }
    }

    /// <summary>
    /// Список тем группы игроку: с принятой заявкой в группе — видит открытую тему, но не скрытую;
    /// с непринятой заявкой или без заявки — ничего.
    /// Раньше доступ проверялся по каждой теме через догрузку <c>Project.Claims</c>, теперь — по
    /// персонажам принятых заявок игрока, загруженным одним запросом.
    /// </summary>
    [Fact]
    public async Task GroupThreadList_ForPlayer_RequiresApprovedClaimInGroup()
    {
        UserIdentification masterId, playerId, applicantId;
        string playerEmail, applicantEmail, strangerEmail;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            (applicantId, applicantEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            (_, strangerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для списка тем игрока");
        }

        var (rootGroupId, threadId, hiddenThreadId, characterId, secondCharacterId) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            var forumService = sp.GetRequiredService<IForumService>();
            var threadId = await forumService.CreateThread(
                projectInfo.GroupTree.RootGroupId,
                "Открытая тема для игроков",
                "Текст темы",
                hideFromUser: false,
                emailEverybody: false);
            var hiddenThreadId = await forumService.CreateThread(
                projectInfo.GroupTree.RootGroupId,
                "Тема только для мастеров",
                "Текст скрытой темы",
                hideFromUser: true,
                emailEverybody: false);

            var characterService = sp.GetRequiredService<ICharacterService>();
            AddCharacterRequest CharacterInRoot() => new(
                projectId,
                ParentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo));
            var characterId = await characterService.AddCharacter(CharacterInRoot());
            var secondCharacterId = await characterService.AddCharacter(CharacterInRoot());
            return (projectInfo.GroupTree.RootGroupId, threadId, hiddenThreadId, characterId, secondCharacterId);
        });

        async Task<ClaimIdentification> AddClaim(UserIdentification userId, CharacterIdentification character)
            => await factory.Services.RunAsAsync(userId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
                return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                    character, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
            });

        var claimId = await AddClaim(playerId, characterId);
        await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));
        // Заявка в обсуждении, не принята.
        _ = await AddClaim(applicantId, secondCharacterId);

        var url = $"{projectId.Value}/roles/{rootGroupId.CharacterGroupId}/forums";
        // Заголовок в разметке закодирован (кириллица), поэтому ищем ссылку на тему.
        var threadLink = $"/forums/{threadId.ThreadId}/ViewThread";
        var hiddenThreadLink = $"/forums/{hiddenThreadId.ThreadId}/ViewThread";

        var playerPage = await (await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), playerEmail))
            .GetStringAsync(url);
        playerPage.ShouldContain(threadLink);
        playerPage.ShouldNotContain(hiddenThreadLink);

        var applicantPage = await (await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), applicantEmail))
            .GetStringAsync(url);
        applicantPage.ShouldNotContain(threadLink);

        var strangerPage = await (await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), strangerEmail))
            .GetStringAsync(url);
        strangerPage.ShouldNotContain(threadLink);
    }

    /// <summary>
    /// c1 чужой видимый, c2 мастера скрытый, c3 игрока, c4 чужой видимый, c5 мастера скрытый,
    /// c6 чужой скрытый, c7 чужой видимый, c8 чужой скрытый. Отметки игрока — c4, c4 (дубль), c2
    /// (ниже максимума); мастера — c1, c1.
    /// </summary>
    private void SeedDiscussion(ProjectIdentification projectId, int discussionId,
        UserIdentification masterId, UserIdentification playerId, UserIdentification otherId)
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        Comment Add(UserIdentification author, bool visible)
        {
            var now = DateTime.UtcNow;
            var comment = new Comment
            {
                ProjectId = projectId.Value,
                CommentDiscussionId = discussionId,
                AuthorUserId = author.Value,
                IsCommentByPlayer = author == playerId,
                IsVisibleToPlayer = visible,
                CreatedAt = now,
                LastEditTime = now,
                CommentText = new CommentText { Text = new MarkdownDbValue("Комментарий для счётчика") },
            };
            _ = ctx.Set<Comment>().Add(comment);
            _ = ctx.SaveChanges();
            return comment;
        }

        var c1 = Add(otherId, visible: true);
        var c2 = Add(masterId, visible: false);
        _ = Add(playerId, visible: true);
        var c4 = Add(otherId, visible: true);
        _ = Add(masterId, visible: false);
        _ = Add(otherId, visible: false);
        _ = Add(otherId, visible: true);
        _ = Add(otherId, visible: false);

        void Mark(UserIdentification user, Comment comment)
            => ctx.Set<ReadCommentWatermark>().Add(new ReadCommentWatermark
            {
                ProjectId = projectId.Value,
                UserId = user.Value,
                CommentDiscussionId = discussionId,
                CommentId = comment.CommentId,
            });

        Mark(playerId, c4);
        Mark(playerId, c4);
        Mark(playerId, c2);
        Mark(masterId, c1);
        Mark(masterId, c1);
        _ = ctx.SaveChanges();
    }

    /// <summary>
    /// Эталон в памяти: видимый комментарий непрочитан, если его id выше максимума из своих комментариев
    /// и своих отметок пользователя в этом обсуждении (0, если нет ни тех, ни других).
    /// </summary>
    private static int ExpectedUnread(CommentDiscussion discussion, UserIdentification userId, bool isMaster)
    {
        var watermark = discussion.Comments.Where(c => c.AuthorUserId == userId.Value).Select(c => c.CommentId)
            .Concat(discussion.Watermarks.Where(w => w.UserId == userId.Value).Select(w => w.CommentId))
            .DefaultIfEmpty(0)
            .Max();
        return discussion.Comments.Count(c => (c.IsVisibleToPlayer || isMaster) && c.CommentId > watermark);
    }

    private CommentDiscussion LoadDiscussion(int discussionId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<CommentDiscussion>()
            .AsNoTracking()
            .Include(d => d.Comments)
            .Include(d => d.Watermarks)
            .Single(d => d.CommentDiscussionId == discussionId);
    }

    private int GetClaimDiscussionId(ClaimIdentification claimId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<Claim>()
            .Where(c => c.ClaimId == claimId.ClaimId)
            .Select(c => c.CommentDiscussionId)
            .Single();
    }

    private int GetThreadDiscussionId(ForumThreadIdentification threadId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<ForumThread>()
            .Where(t => t.ForumThreadId == threadId.ThreadId)
            .Select(t => t.CommentDiscussionId)
            .Single();
    }
}
