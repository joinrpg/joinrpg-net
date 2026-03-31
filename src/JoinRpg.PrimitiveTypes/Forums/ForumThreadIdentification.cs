using System.Text.Json.Serialization;

namespace JoinRpg.PrimitiveTypes.Forums;

[method: JsonConstructor]
[ProjectEntityId(ShortName = "ForumThread", AdditionalPrefixes = ["ForumComment"])]
public partial record class ForumThreadIdentification(ProjectIdentification ProjectId, int ThreadId)
    : ISpanParsable<ForumThreadIdentification>, IProjectEntityId, IComparable<ForumThreadIdentification>
{
    public ForumThreadIdentification(int projectId, int threadId) : this(new ProjectIdentification(projectId), threadId) { }

    public static ForumThreadIdentification? FromOptional(ProjectIdentification projectId, int? threadId)
        => threadId is not null ? new ForumThreadIdentification(projectId, threadId.Value) : null;
}
