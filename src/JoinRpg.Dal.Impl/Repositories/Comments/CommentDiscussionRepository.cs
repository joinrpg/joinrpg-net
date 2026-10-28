using JoinRpg.Data.Interfaces.Comments;
using JoinRpg.DomainTypes.Comments;

namespace JoinRpg.Dal.Impl.Repositories.Comments;

/// <summary>
/// Загрузчик обсуждений (ADR026 §2): один запрос с явной проекцией в строки, затем чистый
/// <see cref="CommentDiscussionMapper"/>.
/// </summary>
/// <remarks>
/// <para>
/// Берёт <see cref="MyDbContext"/> напрямую и не наследуется от <c>GameRepositoryImplBase</c>,
/// как и <c>CharacterInfoLoader</c>: без <c>Include</c>, без ленивых загрузок, без прогрева
/// контекста проектом.
/// </para>
/// <para>
/// Владелец ищется обратным запросом от заявок и тем форума по id обсуждения, а не перебором
/// проекта (#4986). Уникальность ссылки не проверяется: если на обсуждение ссылаются две заявки
/// (или две темы), берётся любая — диагностируется только «и заявка, и тема».
/// </para>
/// <para>
/// Финоперация и текст — внешним соединением по ключу (<c>FirstOrDefault</c>), а не через навигации
/// <c>Comment.Finance</c> / <c>Comment.CommentText</c>: в модели они объявлены обязательными, хотя
/// финоперации у большинства комментариев нет, и по навигации EF6 вправе соединить их INNER JOIN —
/// комментарии без финоперации пропали бы.
/// </para>
/// </remarks>
internal sealed class CommentDiscussionRepository(MyDbContext ctx) : ICommentDiscussionRepository
{
    public async Task<CommentDiscussionInfo?> GetDiscussionOrDefault(CommentDiscussionId discussionId)
    {
        var id = discussionId.Value;
        var claims = ctx.Set<Claim>();
        var threads = ctx.Set<ForumThread>();
        var comments = ctx.Set<Comment>();
        var texts = ctx.Set<CommentText>();
        var finances = ctx.Set<FinanceOperation>();

        var query =
            from discussion in ctx.Set<CommentDiscussion>().AsNoTracking()
            where discussion.CommentDiscussionId == id
            let claim = claims.FirstOrDefault(c => c.CommentDiscussionId == discussion.CommentDiscussionId)
            let thread = threads.FirstOrDefault(t => t.CommentDiscussionId == discussion.CommentDiscussionId)
            select new CommentDiscussionRow
            {
                CommentDiscussionId = discussion.CommentDiscussionId,
                ProjectId = discussion.ProjectId,
                // Каст к nullable превращает обращение к отсутствующей строке в NULL, а не в ошибку.
                ClaimId = (int?)claim.ClaimId,
                ClaimProjectId = (int?)claim.ProjectId,
                ForumThreadId = (int?)thread.ForumThreadId,
                ForumThreadProjectId = (int?)thread.ProjectId,
                ForumThreadCharacterGroupId = (int?)thread.CharacterGroupId,
                ForumThreadIsVisibleToPlayer = (bool?)thread.IsVisibleToPlayer,
                Comments =
                    from comment in comments
                    where comment.CommentDiscussionId == discussion.CommentDiscussionId
                    let text = texts.FirstOrDefault(t => t.CommentId == comment.CommentId)
                    let finance = finances.FirstOrDefault(f => f.CommentId == comment.CommentId)
                    select new CommentRow
                    {
                        CommentId = comment.CommentId,
                        ParentCommentId = comment.ParentCommentId,
                        AuthorUserId = comment.AuthorUserId,
                        CreatedAt = comment.CreatedAt,
                        IsCommentByPlayer = comment.IsCommentByPlayer,
                        IsVisibleToPlayer = comment.IsVisibleToPlayer,
                        ExtraAction = comment.ExtraAction,
                        Text = text.Text.Contents,
                        FinanceMoneyAmount = (int?)finance.MoneyAmount,
                        FinanceOperationType = (FinanceOperationType?)finance.OperationType,
                        FinanceState = (FinanceOperationState?)finance.State,
                        FinanceOperationDate = (DateTime?)finance.OperationDate,
                        FinancePaymentTypeId = finance.PaymentTypeId,
                    },
            };

        var row = await query.SingleOrDefaultAsync();
        return row is null ? null : CommentDiscussionMapper.Map(row);
    }

    public async Task<CommentDiscussionOwner?> GetOwnerOrDefault(CommentDiscussionId discussionId)
    {
        var id = discussionId.Value;
        var row = await SelectOwners(ctx.Set<CommentDiscussion>().Where(discussion => discussion.CommentDiscussionId == id))
            .SingleOrDefaultAsync();
        return row is null ? null : CommentDiscussionMapper.MapOwner(row);
    }

    public async Task<CommentDiscussionOwner?> GetOwnerByCommentOrDefault(CommentId commentId)
    {
        var id = commentId.Value;
        var discussions =
            from comment in ctx.Set<Comment>()
            where comment.CommentId == id
            join discussion in ctx.Set<CommentDiscussion>() on comment.CommentDiscussionId equals discussion.CommentDiscussionId
            select discussion;
        var row = await SelectOwners(discussions).SingleOrDefaultAsync();
        return row is null ? null : CommentDiscussionMapper.MapOwner(row);
    }

    public async Task<CommentId?> GetReadWatermark(CommentDiscussionId discussionId, UserIdentification userId)
    {
        var id = discussionId.Value;
        var userIntId = userId.Value;
        // Каст к int? перед Max: на пустой выборке EF6 иначе падает при материализации.
        var watermark = await ctx.Set<ReadCommentWatermark>()
            .Where(w => w.CommentDiscussionId == id && w.UserId == userIntId)
            .MaxAsync(w => (int?)w.CommentId);
        return CommentId.FromOptional(watermark);
    }

    private IQueryable<CommentDiscussionOwnerRow> SelectOwners(IQueryable<CommentDiscussion> discussions)
    {
        var claims = ctx.Set<Claim>();
        var threads = ctx.Set<ForumThread>();

        return
            from discussion in discussions.AsNoTracking()
            let claim = claims.FirstOrDefault(c => c.CommentDiscussionId == discussion.CommentDiscussionId)
            let thread = threads.FirstOrDefault(t => t.CommentDiscussionId == discussion.CommentDiscussionId)
            select new CommentDiscussionOwnerRow
            {
                CommentDiscussionId = discussion.CommentDiscussionId,
                ProjectId = discussion.ProjectId,
                ClaimId = (int?)claim.ClaimId,
                ClaimProjectId = (int?)claim.ProjectId,
                ForumThreadId = (int?)thread.ForumThreadId,
                ForumThreadProjectId = (int?)thread.ProjectId,
                ForumThreadCharacterGroupId = (int?)thread.CharacterGroupId,
                ForumThreadIsVisibleToPlayer = (bool?)thread.IsVisibleToPlayer,
            };
    }
}
