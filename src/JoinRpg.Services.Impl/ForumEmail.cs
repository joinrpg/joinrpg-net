using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Forums;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Services.Impl;

public record class ForumMessageNotification(
    ForumCommentIdentification ForumCommentId,
    UserInfoHeader Initiator,
    MarkdownString Text,
    string Header,
    UserInfoHeader? ParentCommentAuthor = null
    );
