using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.DataModel
{
  public class Comment : IProjectEntity
  {
    public int CommentId { get; set; }

    public virtual  Project Project { get; set; }
    public int ProjectId { get; set; }

    int IProjectEntity.Id => CommentId;

    public int ClaimId { get; set; }
    public virtual Claim Claim { get; set; }

    public int? ParentCommentId { get; set; }
    public virtual Comment Parent { get; set; }
    public virtual ICollection<Comment> ChildsComments { get; set; }

    public MarkdownString CommentText { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime LastEditTime { get; set; }

    public virtual User Author { get; set; }
    public int AuthorUserId { get; set; }

    public bool IsCommentByPlayer { get; set; }
    public bool IsVisibleToPlayer { get; set; }
  }
}
