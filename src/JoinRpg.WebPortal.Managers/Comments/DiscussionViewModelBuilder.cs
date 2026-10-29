using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Comments;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Comments;
using JoinRpg.Web.Models;
using CommentExtraActionView = JoinRpg.CommonUI.Models.CommentExtraAction;
using CommentViewModel = JoinRpg.Web.Comments.CommentViewModel;

namespace JoinRpg.WebPortal.Managers.Comments;

/// <summary>
/// Собирает модель обсуждения для текущего пользователя из доменного снимка (ADR026 §3).
/// </summary>
/// <remarks>
/// <para>
/// Снимок от пользователя не зависит, а модель — зависит: скрытые от игрока комментарии (вместе с
/// ответами) не-мастеру не попадают в модель вовсе, «новое» считается по отметке прочтения и
/// собственным комментариям, права мастера и модерации финопераций — по <see cref="ProjectInfo"/>.
/// </para>
/// <para>
/// Запросов три, независимо от длины обсуждения: шапки авторов одной пачкой, отметка прочтения и
/// аватарки — одним вызовом <see cref="IAvatarLoader.GetAvatars"/> (вечный кеш, промахи — одним
/// запросом к БД, #5774).
/// </para>
/// </remarks>
internal sealed class DiscussionViewModelBuilder(
    IUserRepository userRepository,
    ICommentDiscussionRepository discussionRepository,
    IAvatarLoader avatarLoader,
    ICurrentUserAccessor currentUserAccessor)
{
    /// <summary>Размер аватарки рядом с комментарием, как и раньше.</summary>
    private const int AvatarSize = 64;

    /// <summary>
    /// Модель обсуждения. Отметку прочтения вызывающий сдвигает уже после — иначе ответы потеряют
    /// пометку «новое» (ADR026, «Попутно найдено»).
    /// </summary>
    /// <param name="discussion">Снимок обсуждения.</param>
    /// <param name="projectInfo">Метаданные проекта обсуждения.</param>
    /// <exception cref="ArgumentException">Обсуждение из другого проекта.</exception>
    public async Task<DiscussionViewModel> Build(CommentDiscussionInfo discussion, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(discussion);
        ArgumentNullException.ThrowIfNull(projectInfo);

        if (discussion.Owner.ProjectId != projectInfo.ProjectId)
        {
            throw new ArgumentException(
                $"Discussion {discussion.Id} belongs to {discussion.Owner.ProjectId}, not to {projectInfo.ProjectId}",
                nameof(projectInfo));
        }

        var currentUserId = currentUserAccessor.UserIdentification;
        var hasMasterAccess = projectInfo.HasMasterAccess(currentUserId);

        var visibleComments = hasMasterAccess
            ? discussion.Comments
            : [.. discussion.Comments.Where(comment => comment.IsVisibleToPlayer)];

        var authors = await LoadAuthors(visibleComments);
        var avatars = await LoadAvatars(authors.Values);
        var watermark = GetWatermark(
            await discussionRepository.GetReadWatermark(discussion.Id, currentUserId),
            discussion.Comments,
            currentUserId);

        // Дерево — за один проход: группировка сохраняет порядок снимка внутри каждой группы.
        // Скрытый комментарий выпал из visibleComments, и его ответы не-мастеру не достанутся:
        // до них не дойдёт обход от корней. Наличие ответов для «скрыть» — по всему снимку.
        var repliesByParent = visibleComments.ToLookup(comment => comment.ParentCommentId);
        var commentsWithReplies = discussion.Comments
            .Select(comment => comment.ParentCommentId)
            .OfType<CommentId>()
            .ToHashSet();

        CommentViewModel ToCommentViewModel(CommentInfo comment, int depth)
        {
            var author = authors[comment.Author];
            return new CommentViewModel(
                comment.Id,
                new UserLinkViewModel(author),
                author.Avatar is { } avatarId ? avatars.GetValueOrDefault(avatarId) : null,
                new DateTimeOffset(DateTime.SpecifyKind(comment.CreatedAt, DateTimeKind.Utc)),
                comment.IsCommentByPlayer,
                comment.IsVisibleToPlayer,
                IsRead: comment.Id.Value <= watermark,
                comment.ExtraAction is { } action ? ((CommentExtraActionView)action).GetDisplayName() : null,
                comment.Text.ToHtmlString().Value,
                comment.Finance is { } finance ? ToFinanceViewModel(finance, projectInfo, currentUserId) : null,
                CanConceal: hasMasterAccess
                    && comment.IsVisibleToPlayer
                    && !comment.IsCommentByPlayer
                    && !commentsWithReplies.Contains(comment.Id),
                depth,
                [.. repliesByParent[comment.Id].Select(reply => ToCommentViewModel(reply, depth + 1))]);
        }

        return new DiscussionViewModel(
            projectInfo.ProjectId,
            discussion.Id,
            hasMasterAccess,
            [.. repliesByParent[null].Select(comment => ToCommentViewModel(comment, 0))]);
    }

    /// <summary>
    /// Прочитано всё до максимума из явной отметки и собственных комментариев пользователя — то же
    /// правило, что у счётчика в SQL (<c>CommentDiscussionPredicates.UnreadCount</c>). Свои комментарии
    /// считаются по всему снимку.
    /// </summary>
    private static int GetWatermark(CommentId? readWatermark, IEnumerable<CommentInfo> comments, UserIdentification userId)
        => comments
            .Where(comment => comment.Author == userId)
            .Select(comment => comment.Id.Value)
            .Append(readWatermark?.Value ?? 0)
            .Max();

    private async Task<IReadOnlyDictionary<UserIdentification, UserInfoHeader>> LoadAuthors(IReadOnlyList<CommentInfo> comments)
    {
        if (comments.Count == 0)
        {
            return new Dictionary<UserIdentification, UserInfoHeader>();
        }
        var headers = await userRepository.GetRequiredUserInfoHeaders([.. comments.Select(comment => comment.Author).Distinct()]);
        return headers.ToDictionary(header => header.UserId);
    }

    /// <summary>
    /// Адреса аватарок авторов. Аватарки, которой нет в БД, в словаре нет — у автора тогда не будет картинки.
    /// </summary>
    private async Task<IReadOnlyDictionary<AvatarIdentification, Uri>> LoadAvatars(IEnumerable<UserInfoHeader> authors)
    {
        var avatarIds = authors.Select(author => author.Avatar).OfType<AvatarIdentification>().Distinct().ToList();
        if (avatarIds.Count == 0)
        {
            return new Dictionary<AvatarIdentification, Uri>();
        }
        var avatars = await avatarLoader.GetAvatars(avatarIds, AvatarSize);
        return avatars.ToDictionary(pair => pair.Key, pair => pair.Value.Uri);
    }

    private static CommentFinanceViewModel ToFinanceViewModel(CommentFinanceInfo finance, ProjectInfo projectInfo, UserIdentification currentUserId)
    {
        var paymentType = finance.PaymentType is { } paymentTypeId
            ? projectInfo.ProjectFinanceSettings.GetPaymentByIdOrDefault(paymentTypeId)
            : null;

        return new CommentFinanceViewModel(
            GetOperationText(finance),
            ((FinanceOperationTypeViewModel)finance.OperationType).GetDisplayName(),
            finance.MoneyAmount,
            DateOnly.FromDateTime(finance.OperationDate),
            paymentType?.GetDisplayName(),
            IsDeclined: finance.State == FinanceOperationState.Declined,
            IsProposed: finance.State == FinanceOperationState.Proposed,
            CanModerate: finance.RequireModeration && projectInfo.CanModerateFinanceOperation(finance.PaymentType, currentUserId));
    }

    private static string? GetOperationText(CommentFinanceInfo finance)
        => finance.MoneyAmount switch
        {
            > 0 when finance.OperationType == FinanceOperationType.TransferFrom => "Перевод денег от другого игрока",
            > 0 => "Оплата денег игроком",
            < 0 when finance.OperationType == FinanceOperationType.TransferTo => "Перевод денег другому игроку",
            < 0 => "Возврат денег игроку",
            _ => null,
        };
}
