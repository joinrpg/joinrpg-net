using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{
  public class ForumThreadViewModel : IEntityWithCommentsViewModel
  {
    public ForumThreadViewModel(ForumThread forumThread, int currentUserId)
    {
      Comments = forumThread.Discussion.ToCommentTreeViewModel(currentUserId);
      ProjectId = forumThread.ProjectId;
      HasMasterAccess = forumThread.HasMasterAccess(currentUserId);
      Header = forumThread.Header;
      ProjectName = forumThread.Project.ProjectName;
      CommentDiscussionId = forumThread.Discussion.CommentDiscussionId;
    }

    public IReadOnlyCollection<CommentViewModel> Comments { get; }

    public int ProjectId { get;}
    public bool HasMasterAccess { get; }

    public string ProjectName { get; }
    public string Header { get; }

    public int CommentDiscussionId { get; }
  }

  public class CreateForumThreadViewModel
  {
    public CreateForumThreadViewModel(CharacterGroup group)
    {
      CharacterGroupId = group.CharacterGroupId;
      CharacterGroupName = group.CharacterGroupName;
      ProjectId = group.ProjectId;
      ProjectName = group.Project.ProjectName;
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public CreateForumThreadViewModel()  { }

    public int CharacterGroupId { get; set; }
    [ReadOnly(true), Display(Name="Группа")]
    public string CharacterGroupName { get; set; }
    public int ProjectId { get; set; }
    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [Required,Display(Name="Заголовок")]
    public string Header { get; set; }

    [Required(ErrorMessage = "Заполните текст комментария"), DisplayName("Текст комментария"), UIHint("MarkdownString")]
    public string CommentText { get; set; }

    [DisplayName("Только для мастеров")]
    public bool HideFromUser { get; set; }
  }
}