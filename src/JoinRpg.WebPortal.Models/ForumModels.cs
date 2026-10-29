using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models;

/// <summary>
/// Шапка страницы темы форума: группа и название. Само обсуждение строится отдельно из доменного
/// снимка (ADR026, шаг 6) и сводится с шапкой в модель страницы в <c>JoinRpg.Portal</c>.
/// </summary>
public class ForumThreadViewModel(ForumThreadHeader thread, CharacterGroupFullInfo characterGroup, ProjectInfo projectInfo, UserIdentification currentUserId)
{
    public CharacterGroupDetailsViewModel GroupDetails { get; } = new CharacterGroupDetailsViewModel(characterGroup, projectInfo, currentUserId, GroupNavigationPage.None);

    public int ProjectId { get; } = thread.ThreadId.ProjectId;

    public string Header { get; } = thread.Header;
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
    public IEnumerable<ForumThreadListItemViewModel> Items { get; } = threads.Select(thread => new ForumThreadListItemViewModel(thread, currentUserId, project)).ToList();
    public string ProjectName { get; } = project.ProjectName;
    public int ProjectId { get; } = project.ProjectId;
    public int RootGroupId { get; } = project.GroupTree.RootGroupId.CharacterGroupId;
    public bool HasMasterAccess { get; } = project.HasMasterAccess(currentUserId);
}

public class ForumThreadListForGroupViewModel(ProjectInfo projectInfo, CharacterGroupFullInfo group, IEnumerable<IForumThreadListItem> threads,
  UserIdentification currentUserId) : ForumThreadListViewModel(projectInfo, threads, currentUserId)
{
    public CharacterGroupDetailsViewModel GroupModel { get; } = new CharacterGroupDetailsViewModel(group, projectInfo, currentUserId, GroupNavigationPage.Forums);
}

public class ForumThreadListItemViewModel
{
    public ForumThreadListItemViewModel(IForumThreadListItem thread, UserIdentification currentUserId, ProjectInfo projectInfo)
    {
        // Права — по ProjectInfo, а не по thread.Project.ProjectAcls: навигация стоила ленивой догрузки ACL (#4989).
        var masterAccess = projectInfo.HasMasterAccess(currentUserId);
        ProjectId = thread.Id.ProjectId;
        Header = thread.Header;
        Topicstarter = thread.Topicstarter;

        LastMessageText = ((MarkdownString?)(masterAccess ? thread.LastMessageText : thread.LastMessageTextForPlayer)).ToHtmlString();
        LastMessageAuthor = masterAccess ? thread.LastMessageAuthor : thread.LastMessageAuthorForPlayer;
        UpdatedAt = thread.UpdatedAt;
        // Оба счётчика посчитаны в запросе для текущего пользователя (ADR026, §2).
        UnreadCount = thread.UnreadCount;
        TotalCount = thread.TotalCount;
        ForumThreadId = thread.Id.ThreadId;
    }

    public int ProjectId { get; }
    public string Header { get; }
    public UserInfoHeader Topicstarter { get; }
    public JoinHtmlString LastMessageText { get; }
    public UserInfoHeader? LastMessageAuthor { get; }
    public DateTime UpdatedAt { get; }
    public int UnreadCount { get; }
    public int TotalCount { get; }
    public int ForumThreadId { get; }
}
