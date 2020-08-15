using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel
{
    public class ReadCommentWatermark
    {
        [Key]
        public int ReadCommentWatermarkId { get; set; }
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; }
        public int CommentDiscussionId { get; set; }

        [ForeignKey(nameof(CommentDiscussionId))]
        public virtual CommentDiscussion CommentDiscussion { get; set; }
        public int CommentId { get; set; }
        public virtual Comment Comment { get; set; }
    }
}

