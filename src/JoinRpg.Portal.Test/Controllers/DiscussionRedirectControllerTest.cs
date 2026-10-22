using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Переход по ссылке на несуществующий комментарий/финоперацию/обсуждение (#5782):
/// мастер получает 404, а не NullReferenceException в CommentRedirectHelper.
/// </summary>
public class DiscussionRedirectControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static DiscussionRedirectController CreateController() =>
        new(new NeverCalledProjectMetadataRepository(), new MissingDiscussionForumRepository(), new FakeCurrentUserAccessor(userId: 1));

    [Fact]
    public async Task ToFinanceOperation_MissingOperation_ReturnsNotFound()
    {
        var result = await CreateController().ToFinanceOperation(ProjectId, financeOperationId: 637699);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToComment_MissingComment_ReturnsNotFound()
    {
        var result = await CreateController().ToComment(ProjectId, commentid: 637699);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToDiscussion_MissingDiscussion_ReturnsNotFound()
    {
        var result = await CreateController().ToDiscussion(ProjectId, commentDiscussionId: 637699);

        result.ShouldBeOfType<NotFoundResult>();
    }

    /// <summary>Репозиторий, в котором нет ни одной дискуссии.</summary>
    private sealed class MissingDiscussionForumRepository : IForumRepository
    {
        public Task<ForumThread> GetThread(ForumThreadIdentification forumThreadId) => throw new NotSupportedException();

        public Task<CommentDiscussion?> GetDiscussion(int projectId, int commentDiscussionId) => Task.FromResult<CommentDiscussion?>(null);

        public Task<CommentDiscussion?> GetDiscussionByComment(int projectId, int commentId) => Task.FromResult<CommentDiscussion?>(null);

        public Task<IReadOnlyCollection<IForumThreadListItem>> GetThreads(int projectId, bool isMaster, IEnumerable<int>? groupIds) => throw new NotSupportedException();

        public Task<ForumThreadHeader> GetThreadHeader(ForumThreadIdentification threadId) => throw new NotSupportedException();
    }

    /// <summary>Если дискуссия не найдена, метаданные проекта не должны даже запрашиваться.</summary>
    private sealed class NeverCalledProjectMetadataRepository : IProjectMetadataRepository
    {
        public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false) => throw new NotSupportedException();

        public Task<global::JoinRpg.DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectIdentification projectId) => throw new NotSupportedException();

        public void PrimeCache(ProjectInfo projectInfo) => throw new NotSupportedException();
    }
}
