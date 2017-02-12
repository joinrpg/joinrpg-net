using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by EF
  public class CommentDiscussion : IProjectEntity, ILinkable
  {
    [Key]
    public int CommentDiscussionId { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();

    public virtual ICollection<ReadCommentWatermark> Watermarks { get; set; }
    int IOrderableEntity.Id => CommentDiscussionId;

    public LinkType LinkType => LinkType.Comment;

    public string Identification => Comments.LastOrDefault()?.Identification;
    int? ILinkable.ProjectId => ProjectId;
  }
}