using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "ForumComment")]
public partial record class ForumCommentIdentification(ForumThreadIdentification ThreadId, int CommentId)
    : ISpanParsable<ForumCommentIdentification>, IProjectEntityId, IComparable<ForumCommentIdentification>
{
    public ForumCommentIdentification(int projectId, int threadId, int commentId)
        : this(new ForumThreadIdentification(new ProjectIdentification(projectId), threadId), commentId)
    {

    }

    public ForumCommentIdentification(ProjectIdentification projectId, int claimId, int commentId) : this(projectId.Value, claimId, commentId)
    {

    }

    public static ForumCommentIdentification? FromOptional(ForumThreadIdentification? threadId, int? commentId)
        => commentId is not null && threadId is not null ? new ForumCommentIdentification(threadId, commentId.Value) : null;
}
