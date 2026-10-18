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

        notifications.GetNotificationLogCalls.ShouldHaveSingleItem().ShouldBe((player, NotificationChannel.Telegram, 100));
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
        notifications.AddLogItem(Item(recipient: new UserIdentification(999), initiator: new UserIdentification(mock.Master.UserId)));

        var item = (await CreateManager().GetLog(recipient: null, channel: null)).Items.ShouldHaveSingleItem();

        item.Recipient.ShouldBe(UserLinkViewModel.Deleted);
        item.Initiator.UserId.ShouldBe(new UserIdentification(mock.Master.UserId));
    }

    [Fact]
    public async Task RendersMarkdownBodyToSanitizedHtml()
    {
        notifications.AddLogItem(Item(
            recipient: new UserIdentification(mock.Player.UserId),
            initiator: new UserIdentification(mock.Master.UserId),
            body: "**важно**<script>alert(1)</script>"));

        var item = (await CreateManager().GetLog(recipient: null, channel: null)).Items.ShouldHaveSingleItem();

        item.BodyHtml.ShouldContain("<strong>важно</strong>");
        item.BodyHtml.ShouldNotContain("<script>");
    }

    private static NotificationLogItemDto Item(UserIdentification recipient, UserIdentification initiator, string body = "body")
        => new(
            new NotificationId(1),
            new NotificationMessageForRecipient(new MarkdownString(body), initiator, "header", recipient, EntityReference: null, DateTimeOffset.UnixEpoch),
            [new NotificationDeliveryDto(NotificationChannel.Email, "Email(player@example.com)", NotificationMessageStatus.Sent, 1, DateTimeOffset.UnixEpoch)]);
}
