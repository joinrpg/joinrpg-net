using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.WebPortal.Managers.AdminTools;

namespace JoinRpg.WebPortal.Managers.Test.AdminTools;

public class NotificationLogManagerTests
{
    private readonly MockedProject mock = new();
    private readonly FakeNotificationRepository notifications = new();

    private NotificationLogManager CreateManager() => new(notifications, new FakeUserRepository(mock));

    [Fact]
    public async Task PassesFiltersAndLimitToRepository()
    {
        var player = new UserIdentification(mock.Player.UserId);

        _ = await CreateManager().GetLog(player, NotificationChannel.Telegram);

        // Лимит + 1: чтобы понять, есть ли что показать по ссылке «Ранее».
        notifications.GetNotificationLogCalls.ShouldHaveSingleItem().ShouldBe((player, NotificationChannel.Telegram, (int?)null, 101));
    }

    [Fact]
    public async Task PassesBeforeToRepository()
    {
        _ = await CreateManager().GetLog(recipient: null, channel: null, before: 42);

        notifications.GetNotificationLogCalls.ShouldHaveSingleItem().ShouldBe(((UserIdentification?)null, (NotificationChannel?)null, (int?)42, 101));
    }

    [Fact]
    public async Task TrimsExtraItemToHasOlderSignal()
    {
        for (var i = 1; i <= 101; i++)
        {
            notifications.AddLogItem(Item(id: i, recipient: new UserIdentification(mock.Player.UserId), initiator: new UserIdentification(mock.Master.UserId)));
        }

        var log = await CreateManager().GetLog(recipient: null, channel: null);

        log.Items.Count.ShouldBe(100);
        log.HasOlder.ShouldBeTrue();
    }

    [Fact]
    public async Task NoHasOlderWhenPageIsFullButNothingBehind()
    {
        notifications.AddLogItem(Item(id: 1, recipient: new UserIdentification(mock.Player.UserId), initiator: new UserIdentification(mock.Master.UserId)));

        var log = await CreateManager().GetLog(recipient: null, channel: null);

        log.HasOlder.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolvesRecipientFilterToUserLink()
    {
        var player = new UserIdentification(mock.Player.UserId);

        var log = await CreateManager().GetLog(player, channel: null);

        log.Recipient.ShouldNotBeNull().UserId.ShouldBe(player);
    }

    [Fact]
    public async Task KeepsRequestedRecipientIdWhenUserIsGone()
    {
        // Аккаунт удалён или слит, а уведомления на его id остались — фильтр не должен теряться.
        var gone = new UserIdentification(999);

        var log = await CreateManager().GetLog(gone, channel: null);

        log.RecipientId.ShouldBe(gone);
        log.Recipient.ShouldBe(UserLinkViewModel.Deleted);
    }

    [Fact]
    public async Task UnknownUserShownAsDeleted()
    {
        notifications.AddLogItem(Item(id: 1, recipient: new UserIdentification(999), initiator: new UserIdentification(mock.Master.UserId)));

        var item = (await CreateManager().GetLog(recipient: null, channel: null)).Items.ShouldHaveSingleItem();

        item.Recipient.ShouldBe(UserLinkViewModel.Deleted);
        item.Initiator.UserId.ShouldBe(new UserIdentification(mock.Master.UserId));
    }

    [Fact]
    public async Task RendersMarkdownBodyToSanitizedHtml()
    {
        notifications.AddLogItem(Item(id: 1,
            recipient: new UserIdentification(mock.Player.UserId),
            initiator: new UserIdentification(mock.Master.UserId),
            body: "**важно**<script>alert(1)</script>"));

        var item = (await CreateManager().GetLog(recipient: null, channel: null)).Items.ShouldHaveSingleItem();

        item.BodyHtml.ShouldContain("<strong>важно</strong>");
        item.BodyHtml.ShouldNotContain("<script>");
    }

    private static NotificationLogItemDto Item(int id, UserIdentification recipient, UserIdentification initiator, string body = "body")
        => new(
            new NotificationId(id),
            new NotificationMessageForRecipient(new MarkdownString(body), initiator, "header", recipient, EntityReference: null, DateTimeOffset.UnixEpoch),
            [new NotificationDeliveryDto(NotificationChannel.Email, "Email(player@example.com)", NotificationMessageStatus.Sent, 1, DateTimeOffset.UnixEpoch)]);
}
