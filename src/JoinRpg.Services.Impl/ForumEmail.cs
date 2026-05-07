using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Forums;

namespace JoinRpg.Services.Impl;

public record class ForumMessageNotification(
    ForumCommentIdentification ForumCommentId,
    UserInfoHeader Initiator,
    MarkdownString Text,
    string Header,
    UserInfoHeader? ParentCommentAuthor = null
    );
