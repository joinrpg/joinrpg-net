using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Notifications;

namespace JoinRpg.Services.Impl.Test;

public class UserMergeNotifierTest
{
    private static readonly UserInfoHeader Primary = new(new UserIdentification(1), new UserDisplayName("Основной", null), Avatar: null);
    private static readonly UserInfoHeader Secondary = new(new UserIdentification(2), new UserDisplayName("Поглощаемый", null), Avatar: null);
    private static readonly UserInfoHeader Admin = new(new UserIdentification(3), new UserDisplayName("Админ", null), Avatar: null);

    [Fact]
    public async Task Secondary_GetsLetterAboutNewAccount()
    {
        var notifications = new FakeNotificationService();
        var notifier = new UserMergeNotifier(notifications, new FakeVirtualUsersService(), new FakeUserRepository(admins: [Admin]));

        await notifier.NotifySecondary(Secondary, "old@example.com", "new@example.com");

        var letter = notifications.Queued.ShouldHaveSingleItem();
        letter.Recepients.ShouldHaveSingleItem().UserId.ShouldBe(Secondary.UserId);
        notifications.DirectChannels.ShouldBe([NotificationChannel.Email]);
        letter.NotificationClass.ShouldBe(NotificationClass.UserAccount);
        letter.TemplateText.TemplateContents.ShouldContain("new@example.com");
        letter.TemplateText.TemplateContents.ShouldContain("old@example.com");
    }

    [Fact]
    public async Task Primary_GetsLetterAboutAbsorbedAccount()
    {
        var notifications = new FakeNotificationService();
        var notifier = new UserMergeNotifier(notifications, new FakeVirtualUsersService(), new FakeUserRepository(admins: [Admin]));

        await notifier.NotifyPrimary(Primary, "old@example.com", "new@example.com");

        var letter = notifications.Queued.ShouldHaveSingleItem();
        letter.Recepients.ShouldHaveSingleItem().UserId.ShouldBe(Primary.UserId);
        notifications.DirectChannels.ShouldBe([NotificationChannel.Email]);
        letter.TemplateText.TemplateContents.ShouldContain("old@example.com");
    }

    [Fact]
    public async Task Admins_GetFullReport()
    {
        var notifications = new FakeNotificationService();
        var notifier = new UserMergeNotifier(notifications, new FakeVirtualUsersService(), new FakeUserRepository(admins: [Admin]));

        await notifier.NotifyAdmins(new UserMergeReport(
            Primary, "new@example.com", Secondary, "old@example.com", Admin.UserId,
            new Dictionary<string, int> { ["Claims.PlayerUserId"] = 3, ["Comments.AuthorUserId"] = 0 },
            ["Телефон"],
            ["Vkontakte"]));

        var report = notifications.Queued.ShouldHaveSingleItem();
        report.NotificationClass.ShouldBe(NotificationClass.AdminMessage);
        report.Recepients.ShouldHaveSingleItem().UserId.ShouldBe(Admin.UserId);
        var text = report.TemplateText.TemplateContents;
        text.ShouldContain("`Claims.PlayerUserId`: 3");
        text.ShouldNotContain("Comments.AuthorUserId"); // нулевые не шумят
        text.ShouldContain("Телефон");
        text.ShouldContain("Vkontakte");
        text.ShouldContain("old@example.com");
        text.ShouldContain("new@example.com");
    }
}
