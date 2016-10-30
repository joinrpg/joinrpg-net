using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.Helpers;
using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by LINQ
  public class Comment : IProjectEntity, IValidatableObject, ILinkable
  {
    public int CommentId { get; set; }

    public virtual  Project Project { get; set; }

    public LinkType LinkType => LinkType.Comment;

    public string Identification => $"{ClaimId}.{CommentId}";

    int? ILinkable.ProjectId => ProjectId;

    public int ProjectId { get; set; }

    int IOrderableEntity.Id => CommentId;

    public int ClaimId { get; set; }
    public virtual Claim Claim { get; set; }

    public int? ParentCommentId { get; set; }
    public virtual Comment Parent { get; set; }
    public IEnumerable<Comment> ChildsComments => Claim.Comments.Where(c => c.ParentCommentId == CommentId);

    [NotNull]
    public virtual CommentText CommentText { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime LastEditTime { get; set; }

    public virtual User Author { get; set; }
    public int AuthorUserId { get; set; }

    public bool IsCommentByPlayer { get; set; }
    public bool IsVisibleToPlayer { get; set; }
    public virtual FinanceOperation Finance { get; set; }

    public CommentExtraAction? ExtraAction { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (Finance != null && !IsVisibleToPlayer)
      {
        yield return new ValidationResult("Finance operations always should be player-visible");
      }

      if (Parent != null && !Parent.IsVisibleToPlayer && IsVisibleToPlayer)
      {
        yield return new ValidationResult("Child comments should not be visible if parent is not");
      }

      if (string.IsNullOrWhiteSpace(CommentText.Text.Contents))
      {
        yield return new ValidationResult("Comment can't be empty", new[] { nameof(CommentText) });
      }
    }
  }

  //Sometimes we need to load bunch of comments
  //i.e. to analyze money 
  //or find problems.
  //But we only need to load comment text on claim page.
  //So this split is win.
  public class CommentText
  {
    public int CommentId { get; set; }
    [NotNull]
    public MarkdownString Text { get; set; } = new MarkdownString();
  }

  public enum CommentExtraAction  
  {
    ApproveFinance,
    RejectFinance,
    ApproveByMaster,
    DeclineByMaster,
    RestoreByMaster,
    MoveByMaster,
    DeclineByPlayer,
    ChangeResponsible,
    NewClaim,
    OnHoldByMaster,
    FeeChanged
  }
}
