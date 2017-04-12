using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by EF
  public class ForumThread : IValidatableObject, IForumThread
  {
    public int ForumThreadId { get; set; }
    public int CharacterGroupId { get; set; }
    [NotNull]
    public virtual CharacterGroup CharacterGroup { get; set; }
    [ForeignKey(nameof(Project))]
    public int ProjectId { get; set; }
    
    public virtual Project Project { get; set; }

    public string Header { get; set; }
    
    public virtual CommentDiscussion CommentDiscussion { get; set; }
    public int CommentDiscussionId { get; set; }
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
      if (CommentDiscussion.Comments.Count == 0)
      {
        yield return new ValidationResult("Must be at least one comment", new [] {nameof(CommentDiscussion.Comments)});
      }
      if (!IsVisibleToPlayer && CommentDiscussion.Comments.Any(comment => comment.IsVisibleToPlayer))
      {
        yield return new ValidationResult("If thread is master only, every comment must be master-only.");
      }
    }

    int IOrderableEntity.Id => ForumThreadId;
    public virtual ICollection<UserForumSubscription> Subscriptions { get; set; } = new List<UserForumSubscription>();
    
  }

  public interface IForumThread : IProjectEntity
  {
    bool IsVisibleToPlayer { get; }
    int CharacterGroupId { get; }
  }

  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by Entity Framework
  public class UserForumSubscription
  {
    public int UserForumSubscriptionId { get; set; }
    public int ForumThreadId { get; set; }
    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; }
  }
}