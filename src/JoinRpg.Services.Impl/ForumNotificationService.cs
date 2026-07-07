using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl;


internal class ForumNotificationService(
    SubscribeCalculator subscribeCalculator,
    INotificationService notificationService,
    IProjectMetadataRepository projectMetadataRepository,
    IForumRepository forumRepository
    )

{

    public async Task SendNotification(ForumMessageNotification model)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(model.ForumCommentId.ProjectId);
        var forumThread = await forumRepository.GetThreadHeader(model.ForumCommentId.ThreadId);

        var header = $"{projectInfo.ProjectName.Value}: тема на форуме {forumThread.Header}";

        var text1 =
$@"Добрый день, %recepient.name%
На форуме появилось новое сообщение:

{model.Text.Contents}
";

        var args = new ForumCalculateArgs(
            Initiator: model.Initiator,
            Groups: [forumThread.CharacterGroupId],
            Masters: [.. projectInfo.Masters.Select(x => x.UserInfo)],
            RespondingTo: []
            );

        await notificationService.QueueNotification(new NotificationEvent(
            NotificationClass.Forum,
            model.ForumCommentId,
            header,
            new NotificationEventTemplate(text1),
            await subscribeCalculator.GetRecepients(args, projectInfo),
            model.Initiator.UserId
            ));
    }
}
