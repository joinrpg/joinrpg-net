using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by EF
  public class ForumThread : IValidatableObject, IProjectEntity
  {
    public int ForumThreadId { get; set; }
    public int CharacterGroupId { get; set; }
    public CharacterGroup CharacterGroup { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; }

    public string Header { get; set; }
    public int CommentDiscussionId { get; set; }
    [ForeignKey(nameof(CommentDiscussionId))]
    public virtual CommentDiscussion Discussion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AuthorUserId))]
    public User AuthorUser { get; set; }
    public int AuthorUserId { get; set; }
    public bool IsVisibleToPlayer { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (string.IsNullOrWhiteSpace(Header))
      {
        yield return new ValidationResult("Header must be not empty", new [] {nameof(Header)});
      }
      if (Discussion.Comments.Count == 0)
      {
        yield return new ValidationResult("Must be at least one comment", new [] {nameof(Discussion.Comments)});
      }
      if (!IsVisibleToPlayer && Discussion.Comments.Any(comment => comment.IsVisibleToPlayer))
      {
        yield return new ValidationResult("If thread is master only, every comment must be master-only.");
      }
    }

    int IOrderableEntity.Id => ForumThreadId;
    public virtual ICollection<User> Subscriptions { get; set; } = new List<User>();
    
  }
}