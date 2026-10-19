using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Forums;

namespace JoinRpg.Services.Impl;

/// <param name="IsVisibleToPlayer">Сообщение видно игрокам: и тема, и сам комментарий не скрыты.
/// Иначе игрокам уведомление не уходит.</param>
public record class ForumMessageNotification(
    ForumCommentIdentification ForumCommentId,
    UserInfoHeader Initiator,
    MarkdownDbValue Text,
    string Header,
    bool IsVisibleToPlayer,
    UserInfoHeader? ParentCommentAuthor = null
    );
