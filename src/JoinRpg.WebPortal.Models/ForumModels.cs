using System.ComponentModel;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models;

public class ForumThreadViewModel(ForumThread forumThread, UserIdentification currentUserId) : IEntityWithCommentsViewModel
{
    public CharacterGroupDetailsViewModel GroupDetails { get; } = new CharacterGroupDetailsViewModel(forumThread.CharacterGroup, currentUserId, GroupNavigationPage.None);

    public IReadOnlyCollection<CommentViewModel> RootComments { get; } = forumThread.CommentDiscussion.ToCommentTreeViewModel(currentUserId);

    public int ProjectId { get; } = forumThread.ProjectId;
    public bool HasMasterAccess { get; } = forumThread.HasMasterAccess(currentUserId);

    public string ProjectName { get; } = forumThread.Project.ProjectName;
    public string Header { get; } = forumThread.Header;

    public int CommentDiscussionId { get; } = forumThread.CommentDiscussionId;
}

public class CreateForumThreadViewModel
{
    public CreateForumThreadViewModel(ProjectInfo project, CharacterGroupInfo group)
    {
        CharacterGroupId = group.Id.CharacterGroupId;
        CharacterGroupName = group.Name;
        ProjectId = project.ProjectId.Value;
        ProjectName = project.ProjectName.Value;
        Header = "";
        CommentText = "";
    }

    public CreateForumThreadViewModel() { }

    public int CharacterGroupId { get; set; }
    [ReadOnly(true), Display(Name = "Группа")]
    public string CharacterGroupName { get; set; }
    public int ProjectId { get; set; }
    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [Required, Display(Name = "Заголовок")]
    public string Header { get; set; }

    [Required(ErrorMessage = "Заполните текст комментария"), DisplayName("Текст комментария"), UIHint("MarkdownString")]
    public string CommentText { get; set; }

    [DisplayName("Только для мастеров")]
    public bool HideFromUser { get; set; }

    [Display(Name = "Послать email", Description = "Уведомить всех, у кого есть доступ к этому форуму по email.")]
    public bool EmailEverybody { get; set; }
}

public class ForumThreadListViewModel(ProjectInfo project, IEnumerable<IForumThreadListItem> threads, UserIdentification currentUserId)
{
    public IEnumerable<ForumThreadListItemViewModel> Items { get; } = threads.Select(thread => new ForumThreadListItemViewModel(thread, currentUserId)).ToList();
    public string ProjectName { get; } = project.ProjectName;
    public int ProjectId { get; } = project.ProjectId;
    public int RootGroupId { get; } = project.RootCharacterGroupId.CharacterGroupId;
    public bool HasMasterAccess { get; } = project.HasMasterAccess(currentUserId);
}

public class ForumThreadListForGroupViewModel(ProjectInfo projectInfo, CharacterGroup group, IEnumerable<IForumThreadListItem> threads,
  UserIdentification currentUserId) : ForumThreadListViewModel(projectInfo, threads, currentUserId)
{
    public CharacterGroupDetailsViewModel GroupModel { get; } = new CharacterGroupDetailsViewModel(group, currentUserId, GroupNavigationPage.Forums);
}

public class ForumThreadListItemViewModel
{
    public ForumThreadListItemViewModel(IForumThreadListItem thread, int currentUserId)
    {
        var masterAccess = thread.HasMasterAccess(new UserIdentification(currentUserId));
        ProjectId = thread.Project.ProjectId;
        Header = thread.Header;
        Topicstarter = thread.Topicstarter;

        LastMessageText = ((MarkdownString?)(masterAccess ? thread.LastMessageText : thread.LastMessageTextForPlayer)).ToHtmlString();
        LastMessageAuthor = masterAccess ? thread.LastMessageAuthor : thread.LastMessageAuthorForPlayer;
        UpdatedAt = thread.UpdatedAt;
        UnreadCount = thread.GetUnreadCount(currentUserId);
        TotalCount = thread.Comments.Count(c => c.IsVisibleTo(currentUserId));
        ForumThreadId = thread.Id;
    }

    public int ProjectId { get; }
    public string Header { get; }
    public User Topicstarter { get; set; }
    public JoinHtmlString LastMessageText { get; }
    public User LastMessageAuthor { get; }
    public DateTime UpdatedAt { get; }
    public int UnreadCount { get; }
    public int TotalCount { get; }
    public int ForumThreadId { get; }
}
