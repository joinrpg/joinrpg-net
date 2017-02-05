using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by EF
  public class CommentDiscussion : IValidatableObject, IProjectEntity, ILinkable
  {
    [Key]
    public int CommentDiscussionId { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; }

    [ForeignKey(nameof(Claim))]
    public int? ClaimId { get; set; }

    [CanBeNull]
    public virtual Claim Claim { get; set; }

    public int? ForumThreadId { get; set; }

    [ForeignKey(nameof(ForumThreadId)),CanBeNull]

    public virtual ForumThread ForumThread { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();

    public virtual ICollection<ReadCommentWatermark> Watermarks { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (Claim == null && ForumThread == null)
      {
        yield return new ValidationResult("Discussion must be linked to something");
      }
    }

    int IOrderableEntity.Id => CommentDiscussionId;
    public LinkType LinkType => ((ILinkable)Comments.LastOrDefault() ?? Claim).LinkType;

    public string Identification => ((ILinkable) Comments.LastOrDefault() ?? Claim).Identification;
    int? ILinkable.ProjectId => ProjectId;
  }
}