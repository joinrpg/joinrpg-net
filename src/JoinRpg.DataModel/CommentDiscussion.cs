using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by EF
public class CommentDiscussion : IProjectEntity, ILinkable
{
    [Key]
    public int CommentDiscussionId { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();

    public virtual ICollection<ReadCommentWatermark> Watermarks { get; set; }
    int IOrderableEntity.Id => CommentDiscussionId;

    public LinkType LinkType => Comments.Any() ? LinkType.Comment : LinkType.CommentDiscussion;

    public string Identification => Comments.Any() ? Comments.Last().Identification : CommentDiscussionId.ToString();
    int? ILinkable.ProjectId => ProjectId;
}
