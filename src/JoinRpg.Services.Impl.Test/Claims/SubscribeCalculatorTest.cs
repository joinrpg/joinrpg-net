using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Ответственный мастер в расчёте получателей уведомления по заявке.
/// </summary>
public class SubscribeCalculatorTest
{
    private readonly MockedProject mock = new();

    private Task<IReadOnlyCollection<NotificationRecepient>> GetRecepients(UserIdentification responsibleMaster)
    {
        var claimsRepository = new FakeClaimsRepository(mock);
        var calculator = new SubscribeCalculator(new FakeUserSubscribeRepository(), new FakeCharacterInfoRepository(mock), claimsRepository);
        return calculator.GetRecepients(
            new SubscribeCalculateArgs(
                Predicate: _ => true,
                Initiator: null,
                Player: [],
                RespMasters: [responsibleMaster],
                Claims: [],
                Characters: [],
                Finance: [],
                RespondingTo: []),
            mock.ProjectInfo);
    }

    [Fact]
    public async Task ActiveResponsibleMaster_IsRecepient()
    {
        var master = new UserIdentification(mock.Master.UserId);

        (await GetRecepients(master)).ShouldContain(r => r.UserId == master && r.SubscriptionReason == SubscriptionReason.ResponsibleMaster);
    }

    [Fact]
    public async Task RemovedResponsibleMaster_IsSkipped()
    {
        // У старой заявки ответственным мог остаться уже снятый мастер (ADR019) — доступа у него нет.
        var removed = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(acl => acl.UserId == removed.UserId).Status = ProjectAclStatus.Removed;
        mock.ReInitProjectInfo();

        (await GetRecepients(new UserIdentification(removed.UserId))).ShouldBeEmpty();
    }

    /// <param name="respondingTo">Автор комментария, на который отвечает мастер.</param>
    private Task<IReadOnlyCollection<NotificationRecepient>> GetForumRecepients(bool isVisibleToPlayer, UserInfoHeader? respondingTo = null)
    {
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);
        var claimsRepository = new FakeClaimsRepository(mock);
        var calculator = new SubscribeCalculator(new FakeUserSubscribeRepository(), new FakeCharacterInfoRepository(mock), claimsRepository);
        return calculator.GetRecepients(
            new ForumCalculateArgs(
                Initiator: mock.Master.ToUserInfoHeader(),
                Groups: [mock.Project.RootGroup.GetId()],
                RespondingTo: respondingTo is null ? [] : [respondingTo],
                Masters: [.. mock.ProjectInfo.Masters.Select(m => m.UserInfo)],
                IsVisibleToPlayer: isVisibleToPlayer),
            mock.ProjectInfo);
    }

    [Fact]
    public async Task VisibleForumMessage_IsSentToPlayers()
    {
        (await GetForumRecepients(isVisibleToPlayer: true)).ShouldContain(r => r.UserId.Value == mock.Player.UserId && r.SubscriptionReason == SubscriptionReason.Forum);
    }

    [Fact]
    public async Task HiddenForumMessage_IsNotSentToPlayers()
    {
        (await GetForumRecepients(isVisibleToPlayer: false)).ShouldNotContain(r => r.UserId.Value == mock.Player.UserId);
    }

    [Fact]
    public async Task ForumReplyToPlayer_IsAnswerToYourComment()
    {
        var player = mock.Player.ToUserInfoHeader();

        (await GetForumRecepients(isVisibleToPlayer: true, respondingTo: player))
            .ShouldContain(r => r.UserId == player.UserId && r.SubscriptionReason == SubscriptionReason.AnswerToYourComment);
    }

    [Fact]
    public async Task HiddenForumReplyToPlayer_IsNotSentToPlayer()
    {
        var player = mock.Player.ToUserInfoHeader();

        (await GetForumRecepients(isVisibleToPlayer: false, respondingTo: player)).ShouldNotContain(r => r.UserId == player.UserId);
    }

    [Fact]
    public async Task HiddenForumReplyToMaster_IsAnswerToYourComment()
    {
        var master = mock.CreateMaster().ToUserInfoHeader();
        mock.ReInitProjectInfo();

        (await GetForumRecepients(isVisibleToPlayer: false, respondingTo: master))
            .ShouldContain(r => r.UserId == master.UserId && r.SubscriptionReason == SubscriptionReason.AnswerToYourComment);
    }

    [Fact]
    public async Task ForumReplyToAuthorOutsideGroup_IsSkipped()
    {
        // Принятой заявки в группе больше нет — тема игроку недоступна, ответ тоже.
        var leftPlayer = new UserInfoHeader(new UserIdentification(100), new UserDisplayName("Ушедший", null), Avatar: null);

        (await GetForumRecepients(isVisibleToPlayer: true, respondingTo: leftPlayer)).ShouldNotContain(r => r.UserId == leftPlayer.UserId);
    }

    [Fact]
    public async Task ForumReplyToRemovedMaster_IsSkipped()
    {
        var removed = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(acl => acl.UserId == removed.UserId).Status = ProjectAclStatus.Removed;
        mock.ReInitProjectInfo();
        var author = removed.ToUserInfoHeader();

        (await GetForumRecepients(isVisibleToPlayer: false, respondingTo: author)).ShouldNotContain(r => r.UserId == author.UserId);
    }
}
