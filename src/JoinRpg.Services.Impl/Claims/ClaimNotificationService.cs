using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Services.Impl.Claims;

internal class ClaimNotificationService(
    IClaimsRepository claimsRepository,
    ClaimNotificationTextBuilder claimNotificationTextBuilder,
    SubscribeCalculator subscribeCalculator,
    INotificationService notificationService,
    IProjectMetadataRepository projectMetadataRepository,
    IVirtualUsersService virtualUsersService
    )

{
    public async Task SendNotification(ClaimSimpleChangedNotification model)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(model.ClaimId.ProjectId);

        var claim = (await claimsRepository.GetClaimHeadersWithPlayer([model.ClaimId])).Single();

        var text1 = claimNotificationTextBuilder.GetText(model, claim);

        var args = new SubscribeCalculateArgs(
            Predicate: GetSubscribePredicate(model.CommentExtraAction),
            Initiator: model.Initiator,
            Player: model.ClaimOperationType == ClaimOperationType.MasterVisibleChange ? [claim.Player] : [],
            RespMasters: [claim.ResponsibleMasterUserId, model.OldResponsibleMaster],
            Claims: [claim.ClaimId],
            Characters: [claim.CharacterId, model.AnotherCharacterId],
            Finance: [model.PaymentOwner],
            RespondingTo: [model.ParentCommentAuthor]
            );

        await notificationService.QueueNotification(new NotificationEvent(
            NotificationClass.Claims,
            model.ClaimId,
            claimNotificationTextBuilder.GetClaimEmailTitle(projectInfo.ProjectName, claim),
            new NotificationEventTemplate(text1),
            await subscribeCalculator.GetRecepients(args, projectInfo),
            model.Initiator.UserId
            ));
    }

    public async Task SendNotification(ClaimOnlinePaymentNotification model)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(model.ClaimId.ProjectId);

        var claim = (await claimsRepository.GetClaimHeadersWithPlayer([model.ClaimId])).Single();

        var args = new SubscribeCalculateArgs(
            Predicate: s => s.MoneyOperation,
            Initiator: null,
            Player: [claim.Player],
            RespMasters: [claim.ResponsibleMasterUserId],
            Claims: [claim.ClaimId],
            Characters: [claim.CharacterId],
            Finance: [],
            RespondingTo: []
            );

        await notificationService.QueueNotification(new NotificationEvent(
            NotificationClass.Claims,
            model.ClaimId,
            claimNotificationTextBuilder.GetClaimEmailTitle(projectInfo.ProjectName, claim),
            model.Text,
            await subscribeCalculator.GetRecepients(args, projectInfo),
            virtualUsersService.PaymentsUser.GetId()
            ));
    }

    internal static Func<SubscriptionOptions, bool> GetSubscribePredicate(CommentExtraAction? commentExtraAction)
    {
        return commentExtraAction switch
        {
            CommentExtraAction.ApproveFinance
                or CommentExtraAction.RejectFinance
                or CommentExtraAction.FeeChanged
                or CommentExtraAction.RequestPreferential
                or CommentExtraAction.PaidFee
                or CommentExtraAction.RefundFee
                or CommentExtraAction.TransferFrom
                or CommentExtraAction.TransferTo
                => s => s.MoneyOperation,
            CommentExtraAction.ApproveByMaster or CommentExtraAction.DeclineByMaster or CommentExtraAction.RestoreByMaster
            or CommentExtraAction.MoveByMaster or CommentExtraAction.DeclineByPlayer or CommentExtraAction.ChangeResponsible
            or CommentExtraAction.OnHoldByMaster or CommentExtraAction.CheckedIn or CommentExtraAction.SecondRole or CommentExtraAction.OutOfGame
            or CommentExtraAction.NewClaim
                => s => s.ClaimStatusChange,
            null => s => s.Comments,
            _ => throw new ArgumentOutOfRangeException(nameof(commentExtraAction), commentExtraAction, "Неожиданное значение")
        };
    }


}
