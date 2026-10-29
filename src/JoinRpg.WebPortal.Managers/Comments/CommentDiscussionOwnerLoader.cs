using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Comments;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.Interfaces;

namespace JoinRpg.WebPortal.Managers.Comments;

/// <summary>
/// Владелец обсуждения вместе с правом текущего пользователя на него.
/// </summary>
/// <param name="Owner">Заявка или тема форума, которой принадлежит обсуждение.</param>
/// <param name="ProjectInfo">Метаданные проекта владельца — он же проект маршрута.</param>
/// <param name="HasAccess">Мастер проекта или игрок, которому обсуждение видно.</param>
public sealed record CommentDiscussionOwnerAccess(CommentDiscussionOwner Owner, ProjectInfo ProjectInfo, bool HasAccess);

/// <summary>
/// Находит владельца обсуждения и проверяет доступ к нему (ADR026 §1, #4986): обратным запросом по
/// id обсуждения, а не перебором <c>Project.Claims</c> / <c>Project.ForumThreads</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>null</c> — обсуждения (комментария) нет или его владелец из другого проекта, чем в маршруте:
/// id обсуждения и комментария глобально уникальны, и без этой проверки мастер одного проекта
/// прошёл бы проверку по обсуждению другого. Вызывающий отвечает 404.
/// </para>
/// <para>
/// Мастеру проекта данные игрока не нужны, поэтому для него, кроме владельца, запросов нет.
/// Игроку заявки — заявка (ADR021), игроку темы — его принятые заявки и их персонажи.
/// </para>
/// </remarks>
public sealed class CommentDiscussionOwnerLoader(
    ICommentDiscussionRepository discussionRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IClaimInfoRepository claimInfoRepository,
    ICharacterInfoRepository characterInfoRepository,
    ICurrentUserAccessor currentUserAccessor)
{
    public async Task<CommentDiscussionOwnerAccess?> GetOwnerOrDefault(ProjectIdentification projectId, CommentDiscussionId discussionId)
        => await CheckAccess(projectId, await discussionRepository.GetOwnerOrDefault(discussionId));

    /// <remarks>Подходит и для финоперации: её ключ и есть id комментария.</remarks>
    public async Task<CommentDiscussionOwnerAccess?> GetOwnerByCommentOrDefault(ProjectIdentification projectId, CommentId commentId)
        => await CheckAccess(projectId, await discussionRepository.GetOwnerByCommentOrDefault(commentId));

    private async Task<CommentDiscussionOwnerAccess?> CheckAccess(ProjectIdentification projectId, CommentDiscussionOwner? owner)
    {
        if (owner is null || owner.ProjectId != projectId)
        {
            return null;
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var hasAccess = currentUserAccessor.UserIdentificationOrDefault is { } userId
            && (projectInfo.HasMasterAccess(userId) || await HasPlayerAccess(owner, userId));
        return new CommentDiscussionOwnerAccess(owner, projectInfo, hasAccess);
    }

    private Task<bool> HasPlayerAccess(CommentDiscussionOwner owner, UserIdentification userId)
        => owner switch
        {
            ClaimDiscussionOwner claimOwner => HasPlayerAccess(claimOwner, userId),
            ForumDiscussionOwner forumOwner => HasPlayerAccess(forumOwner, userId),
            _ => throw new InvalidOperationException($"Unknown comment discussion owner {owner}"),
        };

    private async Task<bool> HasPlayerAccess(ClaimDiscussionOwner owner, UserIdentification userId)
        => await claimInfoRepository.GetClaimInCharacterOrDefault(owner.ClaimId) is { } claim
            && owner.HasPlayerAccess(claim, userId);

    private async Task<bool> HasPlayerAccess(ForumDiscussionOwner owner, UserIdentification userId)
    {
        if (!owner.IsVisibleToPlayer)
        {
            return false;
        }

        // Одним запросом: персонажи принятых заявок игрока, а не «заголовки заявок, потом персонажи».
        var characters = await characterInfoRepository.GetCharacterInfosByPlayer(owner.ProjectId, userId, ClaimStatusSpec.Approved);
        return owner.HasPlayerAccess(characters, userId);
    }
}
