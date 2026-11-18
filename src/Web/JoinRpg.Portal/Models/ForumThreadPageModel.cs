using JoinRpg.Web.Comments;
using JoinRpg.Web.Models;

namespace JoinRpg.Portal.Models;

/// <summary>
/// Страница темы форума: шапка (группа и название) и обсуждение на Razor-компонентах (ADR026, шаг 6).
/// </summary>
public record class ForumThreadPageModel(
    ForumThreadViewModel Thread,
    DiscussionViewModel Discussion);
