using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class AddCommentViewModel
  {
    public int ProjectId { get; set; }
    public int ClaimId { get; set; }
    /// <summary>
    /// Parent comment id
    /// </summary>
    public int? ParentCommentId { get; set; }

    [Required, DisplayName("Текст комментария")] 
    public MarkdownString CommentText { get; set; }

    [DisplayName("Только другим мастерам")]
    public bool HideFromUser
    { get; set; }

    public bool EnableHideFromUser { get; set; }

    public string ActionName { get; set; }
  }
}
