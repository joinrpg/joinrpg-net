using JoinRpg.Data.Interfaces.Comments;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Comments;

namespace JoinRpg.Services.Impl.Comments;

/// <summary>
/// Единый вход записи в обсуждение (ADR026 §5): владельца сервис находит по id обсуждения, общие
/// правила проверяет сам, а комментарий передаёт в операцию заявки или форума.
/// </summary>
/// <remarks>
/// <para>
/// Общие правила — решения из таблицы расхождений ADR026 §5:
/// <list type="bullet">
/// <item>родитель должен быть в этом же обсуждении — иначе отказ, а не ответ в корень (#5853);</item>
/// <item>видимый игроку ответ на скрытый от игрока комментарий — отказ (на форуме раньше ответ молча
/// становился скрытым);</item>
/// <item>(оба правила родителя сервис проверяет у мастера; у остальных — операция, после проверки
/// доступа, см. <see cref="AddComment"/>);</item>
/// <item>скрыть комментарий от игрока может только мастер — раньше это проверяли вызывающие, а сервисы
/// молча делали комментарий видимым.</item>
/// </list>
/// </para>
/// <para>
/// Что осталось разным у заявки и темы, тоже записано там: в архивном проекте заявку комментировать
/// можно (ADR014), а тему — нет; «игрок» для типа операции у заявки — её игрок (мастер на своей заявке
/// пишет как игрок), а у темы — не мастер. Проект для правил ADR023 и активности — у владельца: в
/// <see cref="CommentDiscussionId"/> проекта нет.
/// </para>
/// </remarks>
internal class CommentDiscussionServiceImpl(
    ICommentDiscussionRepository discussionRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    IUnitOfWork unitOfWork,
    IClaimCommentOperation claimCommentOperation,
    IForumCommentOperation forumCommentOperation) : ICommentDiscussionService
{
    public async Task AddComment(CommentDiscussionId discussionId, CommentId? parentCommentId, bool isVisibleToPlayer, string commentText)
    {
        var (owner, discussion) = await LoadOwner(discussionId, withComments: parentCommentId is not null);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(owner.ProjectId);

        _ = owner switch
        {
            // Комментирование заявки в архиве разрешено (ADR014), запрещено только в блокировке (ADR023).
            ClaimDiscussionOwner => projectInfo.EnsureNotBlocked(),
            ForumDiscussionOwner => projectInfo.EnsureProjectActive(),
            _ => throw new InvalidOperationException($"Unknown comment discussion owner {owner}"),
        };

        if (!isVisibleToPlayer)
        {
            _ = projectInfo.RequestMasterAccess(currentUserAccessor);
        }

        // Правила родителя сервис проверяет сам только у мастера: у него доступ к обсуждению есть
        // заведомо. Остальным доступ проверяет операция, и правила родителя она проверяет уже после
        // него — иначе посторонний по коду отказа узнал бы, есть ли в чужом обсуждении такой
        // комментарий и скрыт ли он от игрока.
        if (parentCommentId is not null && discussion is not null && projectInfo.HasMasterAccess(currentUserAccessor))
        {
            var parentComment = discussion.Comments.SingleOrDefault(comment => comment.Id == parentCommentId)
                ?? throw new ParentCommentNotFoundException(parentCommentId.Value);

            if (isVisibleToPlayer && !parentComment.IsVisibleToPlayer)
            {
                throw new VisibleReplyToHiddenCommentException(parentComment.Id.Value);
            }
        }

        switch (owner)
        {
            case ClaimDiscussionOwner claimOwner:
                await claimCommentOperation.AddComment(claimOwner.ClaimId, parentCommentId?.Value, isVisibleToPlayer, commentText);
                break;
            case ForumDiscussionOwner forumOwner:
                await forumCommentOperation.AddComment(forumOwner.ThreadId, parentCommentId?.Value, isVisibleToPlayer, commentText);
                break;
        }
    }

    /// <summary>
    /// Владелец и, если нужны комментарии (это ответ), снимок обсуждения — одним запросом: снимок
    /// содержит владельца.
    /// </summary>
    private async Task<(CommentDiscussionOwner Owner, CommentDiscussionInfo? Discussion)> LoadOwner(
        CommentDiscussionId discussionId,
        bool withComments)
    {
        if (!withComments)
        {
            var owner = await discussionRepository.GetOwnerOrDefault(discussionId)
                ?? throw new JoinRpgEntityNotFoundException(discussionId.Value, nameof(CommentDiscussion));
            return (owner, null);
        }

        var discussion = await discussionRepository.GetDiscussionOrDefault(discussionId)
            ?? throw new JoinRpgEntityNotFoundException(discussionId.Value, nameof(CommentDiscussion));
        return (discussion.Owner, discussion);
    }

    /// <summary>
    /// Сокрытие комментария от игрока.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Операция обсуждения, а не заявки (ADR014 §8): обсуждение может быть форумным, а модерация форума
    /// — не мутация агрегата персонажа.
    /// </para>
    /// <para>
    /// По ADR014 это <c>AllowArchived</c>: модерация должна работать всюду, где работает
    /// комментирование, а комментирование в архивном проекте разрешено. Проверки активности здесь
    /// нет — то есть требование уже выполнено. В заблокированном проекте (ADR023) — отказ.
    /// </para>
    /// </remarks>
    public async Task ConcealComment(CommentId commentId)
    {
        var owner = await discussionRepository.GetOwnerByCommentOrDefault(commentId)
            ?? throw new JoinRpgEntityNotFoundException(commentId.Value, nameof(Comment));

        var discussion = await unitOfWork.GetForumRepository().GetDiscussionByComment(owner.ProjectId.Value, commentId.Value);
        var comment = discussion?.Comments.FirstOrDefault(c => c.CommentId == commentId.Value);

        if (discussion is null || comment is null)
        {
            throw new JoinRpgEntityNotFoundException(commentId.Value, nameof(Comment));
        }

        var childComments = discussion.Comments.Where(c => c.ParentCommentId == commentId.Value);

        // Права — по ProjectInfo, а не по comment.Project.ProjectAcls: навигация стоила ленивой догрузки ACL (#4989).
        var projectInfo = (await projectMetadataRepository.GetProjectMetadata(owner.ProjectId)).EnsureNotBlocked();

        if (projectInfo.HasMasterAccess(currentUserAccessor) && !childComments.Any() &&
            comment.IsVisibleToPlayer && !comment.IsCommentByPlayer)
        {
            comment.IsVisibleToPlayer = false;
            await unitOfWork.SaveChangesAsync();
        }
        else
        {
            throw new JoinRpgConcealCommentException();
        }
    }

    /// <summary>
    /// Отметка «прочитано до такого-то комментария».
    /// </summary>
    /// <remarks>
    /// <para>
    /// Операция обсуждения, а не заявки (ADR014 §8): та же отметка ставится и на темах форума, у
    /// которых заявки нет вовсе.
    /// </para>
    /// <para>
    /// По требованиям ADR014 это <c>AllowArchived</c> (отметка «прочитано» — по смыслу чтение):
    /// проверки активности здесь нет. Отсутствие проверки доступа — известная дыра (#4967).
    /// </para>
    /// </remarks>
    public async Task UpdateReadCommentWatermark(CommentDiscussionId discussionId, CommentId maxCommentId)
    {
        var owner = await discussionRepository.GetOwnerOrDefault(discussionId)
            ?? throw new JoinRpgEntityNotFoundException(discussionId.Value, nameof(CommentDiscussion));

        // Отметку прочтения пишет просмотр страницы, поэтому в заблокированном проекте (ADR023) не бросаем,
        // а молча не пишем: просмотр должен работать, а строки в проекте на восстановлении — не появляться.
        if ((await projectMetadataRepository.GetProjectMetadata(owner.ProjectId)).ProjectStatus == ProjectLifecycleStatus.Blocked)
        {
            return;
        }

        var currentUserId = currentUserAccessor.UserId;
        var watermarks =
          unitOfWork.GetDbSet<ReadCommentWatermark>()
            .Where(w => w.CommentDiscussionId == discussionId.Value && w.UserId == currentUserId)
            .OrderByDescending(wm => wm.ReadCommentWatermarkId)
            .ToList();

        //Sometimes watermarks can duplicate. If so, let's remove them.
        foreach (var wm in watermarks.Skip(1))
        {
            _ = unitOfWork.GetDbSet<ReadCommentWatermark>().Remove(wm);
        }

        var watermark = watermarks.FirstOrDefault();

        if (watermark == null)
        {
            watermark = new ReadCommentWatermark()
            {
                CommentDiscussionId = discussionId.Value,
                ProjectId = owner.ProjectId.Value,
                UserId = currentUserId,
            };
            _ = unitOfWork.GetDbSet<ReadCommentWatermark>().Add(watermark);
        }

        if (watermark.CommentId > maxCommentId.Value)
        {
            return;
        }
        watermark.CommentId = maxCommentId.Value;
        await unitOfWork.SaveChangesAsync();
    }
}
