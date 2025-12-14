using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Forums;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl;


internal class ForumNotificationService(
    SubscribeCalculator subscribeCalculator,
    INotificationService notificationService,
    IProjectMetadataRepository projectMetadataRepository,
    IForumRepository forumRepository,
    INotificationUriLocator<ForumThreadIdentification> uriLocator
    )

{

    public async Task SendNotification(ForumMessageNotification model)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(model.ForumCommentId.ProjectId);
        var forumThread = await forumRepository.GetThreadHeader(model.ForumCommentId.ThreadId);

        var header = $"{projectInfo.ProjectName}: тема на форуме {forumThread.Header}";

        var text1 =
$@"Добрый день, %recepient.name%
На форуме появилось новое сообщение: 

{model.Text.Contents}

Чтобы ответить на комментарий, перейдите на страницу обсуждения: {uriLocator.GetUri(model.ForumCommentId.ThreadId)}
";

        var args = new ForumCalculateArgs(
            Initiator: model.Initiator,
            Groups: [forumThread.CharacterGroupId],
            Masters: [.. projectInfo.Masters.Select(x => x.UserInfo)],
            RespondingTo: []
            );

        await notificationService.QueueNotification(new NotificationEvent(
            NotificationClass.Forum,
            model.ForumCommentId.ThreadId,
            header,
            new NotificationEventTemplate(text1),
            await subscribeCalculator.GetRecepients(args, projectInfo),
            model.Initiator.UserId
            ));
    }
}
