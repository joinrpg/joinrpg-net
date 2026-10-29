using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Comments;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Comments;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Comments;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Portal.Models;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.WebPortal.Managers.Comments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{projectId}/forums/{forumThreadId}/[action]")]
public class ForumController(
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterGroupRepository charGroupRepository,
    IForumService forumService,
    IForumRepository forumRepository,
    IClaimsRepository claimsRepository,
    ICurrentUserAccessor currentUserAccessor,
    ICommentDiscussionService commentDiscussionService,
    CommentDiscussionOwnerLoader ownerLoader,
    ICommentDiscussionRepository commentDiscussionRepository,
    ICharacterInfoRepository characterInfoRepository,
    DiscussionViewModelBuilder discussionViewModelBuilder) : JoinControllerGameBase
{
    [HttpGet("~/{projectId}/roles/{charactergroupid}/create-thread")]
    [ProjectShouldBeActive]
    [MasterAuthorize]
    public async Task<ActionResult> CreateThread(ProjectIdentification projectId, int charactergroupid)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var group = projectInfo.GetGroupById(charactergroupid);
        if (group is null)
        {
            return NotFound();
        }
        return View(new CreateForumThreadViewModel(projectInfo, group));
    }

    [HttpPost("~/{projectId}/roles/{charactergroupid}/create-thread")]
    [ProjectShouldBeActive]
    [MasterAuthorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateThread(CreateForumThreadViewModel viewModel)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));
        var group = projectInfo.GetGroupById(viewModel.CharacterGroupId);

        viewModel.CharacterGroupName = group.Name;
        viewModel.ProjectName = projectInfo.ProjectName.Value;

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }
        try
        {
            var forumThreadId = await forumService.CreateThread(
              new(viewModel.ProjectId, viewModel.CharacterGroupId),
              viewModel.Header,
              viewModel.CommentText,
              viewModel.HideFromUser,
              viewModel.EmailEverybody);
            return RedirectToAction("ViewThread", new { ProjectId = forumThreadId.ProjectId.Value, ForumThreadId = forumThreadId.ThreadId });
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<ActionResult> ViewThread(ForumThreadIdentification? forumThreadId)
    {
        // Голое число из маршрута {forumThreadId} биндер склеивает с проектом маршрута; не распознал — null.
        if (forumThreadId is null)
        {
            return NotFound();
        }

        // Заголовок темы без комментариев: обсуждение грузится доменным снимком ниже, один раз (ADR026, шаг 6).
        var thread = await forumRepository.GetThreadHeaderOrDefault(forumThreadId);
        if (thread is null)
        {
            return NotFound();
        }

        // Доступ — по владельцу обсуждения, как у CreateComment: мастер или игрок с принятой заявкой
        // на персонажа из группы видимой игрокам темы (ForumDiscussionOwner.HasPlayerAccess).
        var access = await ownerLoader.CheckAccess(forumThreadId.ProjectId, thread.ToDiscussionOwner());
        if (access is null)
        {
            return NotFound();
        }
        if (!access.HasAccess)
        {
            throw new NoAccessToProjectException(access.ProjectInfo, currentUserAccessor.UserIdentification);
        }

        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(thread.CharacterGroupId);
        var discussion = await commentDiscussionRepository.GetDiscussionOrDefault(thread.CommentDiscussionId);
        if (charGroupFullInfo is null || discussion is null)
        {
            return NotFound();
        }

        // Модель (с пометками «новое» на всём дереве) строится целиком до сдвига отметки прочтения:
        // построитель читает отметку сам, и сдвиг после него на пометки уже не влияет.
        var discussionModel = await discussionViewModelBuilder.Build(discussion, access.ProjectInfo);

        // Как на странице заявки (ClaimController.ShowClaim): в заблокированном проекте (ADR023)
        // сервис молча не пишет отметку. Доступ к теме проверен выше. Непрочитанное ищем среди видимого
        // пользователю: если у игрока новые только скрытые комментарии, отметку не трогаем — на счётчики
        // это не влияет, а при первом видимом новом она всё равно уйдёт на последний комментарий.
        if (discussionModel.HasUnread())
        {
            await commentDiscussionService.UpdateReadCommentWatermark(
                discussion.Id,
                discussion.Comments.MaxBy(comment => comment.Id.Value)!.Id);
        }

        return View(new ForumThreadPageModel(
            new ForumThreadViewModel(thread, charGroupFullInfo, access.ProjectInfo, currentUserAccessor.UserIdentification),
            discussionModel));
    }

    [HttpPost("~/{projectId}/forums/createcomment")]
    public async Task<ActionResult> CreateComment(AddCommentViewModel viewModel)
    {
        var access = await ownerLoader.GetOwnerOrDefault(
            new ProjectIdentification(viewModel.ProjectId),
            new CommentDiscussionId(viewModel.CommentDiscussionId));
        if (access is null)
        {
            return NotFound();
        }

        if (!access.HasAccess)
        {
            throw new NoAccessToProjectException(access.ProjectInfo, currentUserAccessor.UserIdentification);
        }

        try
        {
            // Подтверждение и отклонение финопераций — в диалоге модерации (FinanceModerationButton), не здесь.
            // Скрыть от игрока может только мастер — это, как и остальные правила, проверяет сервис.
            await commentDiscussionService.AddComment(
                new CommentDiscussionId(viewModel.CommentDiscussionId),
                viewModel.ParentCommentId is int parentId ? new CommentId(parentId) : null,
                !viewModel.HideFromUser,
                viewModel.CommentText);

            return CommentRedirectHelper.RedirectToDiscussion(Url, access.Owner);
        }
        catch
        {
            //TODO: Message that comment is not added
            return CommentRedirectHelper.RedirectToDiscussion(Url, access.Owner);
        }

    }

    [HttpGet("~/{projectId}/forums")]
    public async Task<ActionResult> ListThreads(ProjectIdentification projectid)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectid);
        var isMaster = project.HasMasterAccess(currentUserAccessor);
        IEnumerable<int>? groupIds;
        if (isMaster)
        {
            groupIds = null;
        }
        else
        {
            var claims = await claimsRepository.GetClaimsForPlayer(projectid, currentUserAccessor.UserIdentification, ClaimStatusSpec.Approved);

            groupIds = claims.SelectMany(claim => claim.Character.GetParentGroupIdsToTop(project).Select(g => g.CharacterGroupId));
        }
        var threads = await forumRepository.GetThreads(projectid.Value, currentUserAccessor.UserIdentification, isMaster, groupIds);
        var viewModel = new ForumThreadListViewModel(project, threads, currentUserAccessor.UserIdentification);
        return View(viewModel);
    }

    [HttpGet("~/{projectId}/roles/{characterGroupId}/forums")]
    public async Task<ActionResult> ListThreadsByGroup(ProjectIdentification projectid, int characterGroupId)
    {
        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(new CharacterGroupIdentification(projectid, characterGroupId));
        if (charGroupFullInfo == null)
        {
            return NotFound();
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectid);
        var isMaster = projectInfo.HasMasterAccess(currentUserAccessor);
        var userId = currentUserAccessor.UserIdentification;
        IEnumerable<IForumThreadListItem> threads = await forumRepository.GetThreads(projectid.Value, userId, isMaster, new[] { characterGroupId });
        if (!isMaster && threads.Any())
        {
            // То же правило, что у владельца обсуждения (ForumDiscussionOwner.HasPlayerAccess в
            // CommentDiscussionOwnerLoader), — по персонажам принятых заявок игрока одним запросом,
            // а не через догрузку Project.Claims на каждой теме.
            var characters = await characterInfoRepository.GetCharacterInfosByPlayer(projectid, userId, ClaimStatusSpec.Approved);
            threads = threads
                .Where(thread => new ForumDiscussionOwner(thread.Id, new CharacterGroupIdentification(projectid, thread.CharacterGroupId), thread.IsVisibleToPlayer)
                    .HasPlayerAccess(characters, userId))
                .ToList();
        }
        var viewModel = new ForumThreadListForGroupViewModel(projectInfo, charGroupFullInfo, threads, userId);
        return View(viewModel);
    }

    [HttpPost("~/{projectId}/forums/concealcomment")]
    public async Task<ActionResult> ConcealComment(ProjectIdentification projectid, CommentId? commentid)
    {
        // Голое число из формы биндится через TypeConverter типа (см. CommentIdModelBindingTest).
        if (commentid is null)
        {
            return NotFound();
        }

        // Владелец — для сверки с проектом маршрута и адреса перехода: права на скрытие проверяет сервис.
        var owner = await commentDiscussionRepository.GetOwnerByCommentOrDefault(commentid);
        if (owner is null || owner.ProjectId != projectid)
        {
            return NotFound();
        }
        await commentDiscussionService.ConcealComment(commentid);
        return CommentRedirectHelper.RedirectToDiscussion(Url, owner);
    }
}
