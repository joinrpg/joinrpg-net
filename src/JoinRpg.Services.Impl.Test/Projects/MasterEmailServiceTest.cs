using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test.Projects;

public class MasterEmailServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeNotificationService notificationService = new();

    private MasterEmailService CreateService()
        => new(new MastersNotificationService(notificationService, new FakeUserRepository()),
            new FakeProjectMetadataRepository(mock), new FakeVirtualUsersService());

    [Fact]
    public async Task EmailProjectBlockedReminder_SendsToAllMasters()
    {
        var extraMaster = mock.CreateMaster();
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();

        await CreateService().EmailProjectBlockedReminder(new() { ProjectId = new(mock.Project.ProjectId) });

        var letter = notificationService.Queued.ShouldHaveSingleItem();
        letter.NotificationClass.ShouldBe(NotificationClass.MasterProject);
        letter.Initiator.ShouldBe(new UserIdentification(int.MaxValue)); // робот
        letter.Header.ShouldBe("Mocked project: проект заблокирован на время восстановления данных");
        letter.EntityReference.ShouldBe(new ProjectIdentification(mock.Project.ProjectId));
        letter.Recepients.Select(r => r.UserId).ShouldBe(
            [new UserIdentification(mock.Master.UserId), new UserIdentification(extraMaster.UserId)],
            ignoreOrder: true);

        var text = letter.TemplateText.TemplateContents;
        text.ShouldContain("Добрый день, %recepient.name%!");
        text.ShouldContain("Mocked project");
        text.ShouldContain("https://docs.joinrpg.ru/recovery-2026.html");
        text.ShouldContain("https://joinrpg.ru/support");
    }

    /// <summary>Снапшот итогового текста (ADR003): ловит баги интерполяции, которые ShouldContain не видит.</summary>
    [Fact]
    public async Task EmailProjectBlockedReminder_TextSnapshot()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();

        await CreateService().EmailProjectBlockedReminder(new() { ProjectId = new(mock.Project.ProjectId) });

        var letter = notificationService.Queued.ShouldHaveSingleItem();
        await Verify(new
        {
            letter.Header,
            Text = letter.TemplateText.TemplateContents,
        });
    }

    [Fact]
    public async Task EmailBudgetExpensesAlert_SendsOnlyToFinanceAdmins()
    {
        var masterWithoutMoney = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(a => a.UserId == masterWithoutMoney.UserId).CanManageMoney = false;
        mock.ReInitProjectInfo();

        await CreateService().EmailBudgetExpensesAlert(new() { ProjectId = new(mock.Project.ProjectId) });

        var letter = notificationService.Queued.ShouldHaveSingleItem();
        letter.NotificationClass.ShouldBe(NotificationClass.MasterProject);
        letter.Initiator.ShouldBe(new UserIdentification(int.MaxValue)); // робот
        letter.Header.ShouldBe("Mocked project: в бюджете не зафиксированы расходы за прошлый месяц");
        letter.EntityReference.ShouldBe(new ProjectIdentification(mock.Project.ProjectId));
        letter.Recepients.Select(r => r.UserId).ShouldBe(
            [new UserIdentification(mock.Master.UserId)]);

        var text = letter.TemplateText.TemplateContents;
        text.ShouldContain("Mocked project");
        text.ShouldContain("https://joinrpg.ru/" + mock.Project.ProjectId + "/money/expenses");
    }

    /// <summary>Снапшот итогового текста (ADR003): ловит баги интерполяции, которые ShouldContain не видит.</summary>
    [Fact]
    public async Task EmailBudgetExpensesAlert_TextSnapshot()
    {
        await CreateService().EmailBudgetExpensesAlert(new() { ProjectId = new(mock.Project.ProjectId) });

        var letter = notificationService.Queued.ShouldHaveSingleItem();
        await Verify(new
        {
            letter.Header,
            Text = letter.TemplateText.TemplateContents,
        });
    }
}
