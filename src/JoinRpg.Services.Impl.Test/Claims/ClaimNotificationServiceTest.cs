using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Получатели уведомления об ответе на комментарий к заявке: автор комментария, на который
/// отвечают, получает уведомление, только если ответ ему виден (#5497).
/// </summary>
public class ClaimNotificationServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeNotificationService notificationService = new();
    private readonly FakeClaimsRepository claimsRepository;
    private readonly ClaimWithPlayer claim;

    /// <summary>Отвечает мастер, не ответственный за заявку.</summary>
    private readonly UserInfoHeader initiator;

    public ClaimNotificationServiceTest()
    {
        claimsRepository = new FakeClaimsRepository(mock);
        initiator = mock.CreateMaster().ToUserInfoHeader();
        mock.ReInitProjectInfo();

        var character = mock.CreateCharacter("Персонаж");
        var dbClaim = mock.CreateClaim(character, mock.Player);
        claim = new ClaimWithPlayer
        {
            ClaimId = dbClaim.GetId(),
            CharacterName = "Персонаж",
            Player = mock.Player.ToUserInfoHeader(),
            ExtraNicknames = null,
            ResponsibleMasterUserId = new UserIdentification(mock.Master.UserId),
            CharacterId = character.GetId(),
        };
        claimsRepository.Headers.Add(claim);
    }

    private async Task<IReadOnlyCollection<NotificationRecepient>> SendReply(UserInfoHeader parentCommentAuthor, ClaimOperationType operationType)
    {
        var service = new ClaimNotificationService(
            claimsRepository,
            new ClaimNotificationTextBuilder(),
            new SubscribeCalculator(new FakeUserSubscribeRepository(), new FakeCharacterInfoRepository(mock), claimsRepository),
            notificationService,
            new FakeProjectMetadataRepository(mock),
            new FakeVirtualUsersService());

        await service.SendNotification(new ClaimSimpleChangedNotification(
            claim.ClaimId,
            CommentExtraAction: null,
            initiator,
            new NotificationEventTemplate("Ответ"),
            operationType,
            ParentCommentAuthor: parentCommentAuthor));

        return notificationService.Queued.Single().Recepients;
    }

    [Fact]
    public async Task SecretReplyToPlayerComment_IsNotSentToPlayer()
    {
        var recepients = await SendReply(claim.Player, ClaimOperationType.MasterSecretChange);

        recepients.ShouldNotContain(r => r.UserId == claim.Player.UserId);
    }

    [Fact]
    public async Task VisibleReplyToPlayerComment_IsSentToPlayer()
    {
        var recepients = await SendReply(claim.Player, ClaimOperationType.MasterVisibleChange);

        recepients.ShouldContain(r => r.UserId == claim.Player.UserId && r.SubscriptionReason == SubscriptionReason.AnswerToYourComment);
    }

    [Fact]
    public async Task SecretReplyToMasterComment_IsSentToMaster()
    {
        var parentAuthor = mock.CreateMaster().ToUserInfoHeader();
        mock.ReInitProjectInfo();

        var recepients = await SendReply(parentAuthor, ClaimOperationType.MasterSecretChange);

        recepients.ShouldContain(r => r.UserId == parentAuthor.UserId && r.SubscriptionReason == SubscriptionReason.AnswerToYourComment);
    }
}
