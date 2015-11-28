using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
  public class Comment : IProjectEntity, IValidatableObject
  {
    public int CommentId { get; set; }

    public virtual  Project Project { get; set; }
    public int ProjectId { get; set; }

    int IOrderableEntity.Id => CommentId;

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
    public virtual FinanceOperation Finance { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (Finance != null && !IsVisibleToPlayer)
      {
        yield return new ValidationResult("Finance operations always should be player-visible");
      }

      if (string.IsNullOrWhiteSpace(CommentText.Contents))
      {
        yield return new ValidationResult("Comment can't be empty");
      }

      if (IsCommentByPlayer != (Claim.PlayerUserId == AuthorUserId))
      {
        yield return new ValidationResult("IsCommentByPlayer filled incorrectly");
      }
    }
  }
}
