using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes;

/// <summary>
/// Комментарий в обсуждении заявки или темы форума (ADR026).
/// </summary>
/// <remarks>
/// Не <c>IProjectEntityId</c> — по той же причине, что и <see cref="CommentDiscussionId"/>: без
/// владельца обсуждения у комментария нет адреса, а id уникален в БД сам по себе. Адресные id
/// (<c>ClaimCommentIdentification</c>, <c>ForumCommentIdentification</c>) строит владелец:
/// <c>CommentDiscussionOwner.GetCommentIdentification</c>.
/// </remarks>
[method: JsonConstructor]
[TypedEntityId]
public partial record CommentId(int Value);
