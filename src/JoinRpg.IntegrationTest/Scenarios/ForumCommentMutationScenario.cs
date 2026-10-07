using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// POST-ручки комментариев форума под мастером: добавить комментарий и скрыть его от игроков.
/// </summary>
/// <remarks>
/// Смоук по страницам (<see cref="AllGetPagesSmokeScenario"/>) ходит только по GET, а в прод-логах
/// <c>forums/createcomment</c> и <c>forums/ConcealComment</c> догружали <c>ProjectAcls</c>: права
/// проверялись через навигацию <c>discussion.Project.ProjectAcls</c>, а не по <c>ProjectInfo</c>
/// (#4989). Проверку ленивых загрузок делает <c>LazyLoadAssertingHandler</c> на клиенте фабрики:
/// число догрузок по обоим маршрутам сверяется с <c>lazy-loads-baseline.json</c>
/// (см. docs/lazy-loads-baseline.md).
/// </remarks>
public class ForumCommentMutationScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task ForumCommentMutations_WorkUnderMaster()
    {
        UserIdentification masterId;
        string email;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для комментариев форума");
        }

        var threadId = await factory.Services.RunAsAsync(
            masterId,
            async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
                return await sp.GetRequiredService<IForumService>().CreateThread(
                    projectInfo.GroupTree.RootGroupId,
                    "Тема для комментариев",
                    "Первый комментарий в теме",
                    hideFromUser: false,
                    emailEverybody: false);
            });

        var discussionId = GetDiscussionId(threadId);

        // Редиректы намеренно не проходим: успех — 302 на тему, а переход по нему замерил бы
        // ленивые загрузки уже другого маршрута.
        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            email,
            followsRedirects: false);

        var project = projectId.Value;
        var threadUrl = $"{project}/forums/{threadId.ThreadId}/ViewThread";

        var token = await client.GetAntiforgeryTokenAsync(threadUrl);
        var created = await client.PostFormAsync(
            $"{project}/forums/createcomment",
            token,
            ("ProjectId", project.ToString()),
            ("CommentDiscussionId", discussionId.ToString()),
            ("CommentText", "Комментарий через ручку"),
            ("HideFromUser", "false"));
        created.StatusCode.ShouldBe(HttpStatusCode.Found);

        // Ручка на ошибке тоже отвечает редиректом, поэтому успех проверяем по базе.
        var commentId = GetCommentId(discussionId, "Комментарий через ручку")
            ?? throw new ShouldAssertException("Комментарий не добавлен");

        token = await client.GetAntiforgeryTokenAsync(threadUrl);
        var concealed = await client.PostFormAsync(
            $"{project}/forums/concealcomment?commentId={commentId}&commentDiscussionId={discussionId}",
            token);
        concealed.StatusCode.ShouldBe(HttpStatusCode.Found);

        IsVisibleToPlayer(commentId).ShouldBeFalse();
    }

    private int GetDiscussionId(ForumThreadIdentification threadId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<ForumThread>()
            .Where(t => t.ForumThreadId == threadId.ThreadId)
            .Select(t => t.CommentDiscussionId)
            .Single();
    }

    private int? GetCommentId(int discussionId, string text)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<Comment>()
            .Where(c => c.CommentDiscussionId == discussionId && c.CommentText.Text.Contents == text)
            .Select(c => (int?)c.CommentId)
            .SingleOrDefault();
    }

    private bool IsVisibleToPlayer(int commentId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<Comment>()
            .Where(c => c.CommentId == commentId)
            .Select(c => c.IsVisibleToPlayer)
            .Single();
    }
}
