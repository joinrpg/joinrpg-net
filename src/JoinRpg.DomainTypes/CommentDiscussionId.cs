using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes;

/// <summary>
/// Обсуждение — лента комментариев заявки или темы форума.
/// </summary>
/// <remarks>
/// Не <c>IProjectEntityId</c>: своей страницы у обсуждения нет, это часть заявки или темы, поэтому и
/// адреса (<c>IUriLocator</c>) у него быть не может. Id обсуждения уникален в БД сам по себе.
/// </remarks>
[method: JsonConstructor]
[TypedEntityId]
public partial record CommentDiscussionId(int Value);
