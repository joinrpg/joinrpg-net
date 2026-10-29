using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Markdown;
using JoinRpg.Web.Claims.Finance;
using CommentExtraAction = JoinRpg.CommonUI.Models.CommentExtraAction;

namespace JoinRpg.Web.Models;

public class CommentViewModel
{
    public CommentViewModel(CommentDiscussion parent, Comment comment, UserIdentification currentUserId, ProjectInfo projectInfo, int deepLevel)
    {
        DeepLevel = deepLevel;
        IsVisibleToPlayer = comment.IsVisibleToPlayer;
        // Права — по ProjectInfo, а не по comment.Project.ProjectAcls: навигация стоила ленивой догрузки ACL (#4989).
        HasMasterAccess = projectInfo.HasMasterAccess(currentUserId);
        CanModerateFinance = comment.Finance is not null
            && projectInfo.CanModerateFinanceOperation(comment.Finance.GetPaymentTypeId(), currentUserId);
        IsCommentByPlayer = comment.IsCommentByPlayer;
        Author = new UserLinkViewModel(comment.Author.ToUserInfoHeader());
        AuthorAvatar = AvatarIdentification.FromOptional(comment.Author.SelectedAvatarId);
        AuthorEmail = comment.Author.Email;
        CreatedTime = comment.CreatedAt;
        Finance = comment.Finance;
        CommentText = ((MarkdownString?)comment.CommentText.Text).ToHtmlString();
        CommentId = comment.CommentId;
        ProjectId = comment.ProjectId;
        CommentDiscussionId = comment.CommentDiscussionId;
        IsRead = comment.IsReadByUser(currentUserId.Value);
        // Ответы строятся сразу, а не отложенно при рендере: контроллер сдвигает отметку прочтения
        // после построения модели, и «новое» у ответов должно считаться до этого (ADR026, шаг 3).
        ChildComments =
          parent.Comments.Where(c => c.ParentCommentId == comment.CommentId)
            .Select(c => new CommentViewModel(parent, c, currentUserId, projectInfo, deepLevel + 1))
            .OrderBy(c => c.CreatedTime)
            .ToList();
        ExtraAction = comment.ExtraAction == null ? null : (CommentExtraAction?)comment.ExtraAction.Value;
        IsVisible = comment.IsVisibleTo(currentUserId.Value, projectInfo);
        FinanceModeration = ShowFinanceModeration ? comment.Finance!.ToModerationView(projectInfo) : null;
    }

    /// <summary>Сводка для диалога модерации, если текущий пользователь может подтвердить операцию.</summary>
    public FinanceModerationOperationView? FinanceModeration { get; }

    public bool IsRead { get; }
    public bool IsVisibleToPlayer { get; }
    public bool HasMasterAccess { get; }
    private bool CanModerateFinance { get; }
    public bool IsCommentByPlayer { get; }

    public AvatarIdentification? AuthorAvatar { get; }
    public string AuthorEmail { get; }
    public UserLinkViewModel Author { get; }
    public DateTime CreatedTime { get; }
    public FinanceOperation? Finance { get; }
    public JoinHtmlString CommentText { get; }
    public int CommentId { get; }
    public IReadOnlyList<CommentViewModel> ChildComments { get; }
    public int ProjectId { get; }
    public int CommentDiscussionId { get; }
    public CommentExtraAction? ExtraAction { get; set; }

    public bool ShowFinanceModeration => Finance != null && Finance.RequireModeration && CanModerateFinance;

    public bool IsVisible { get; }
    public int DeepLevel { get; }
}

