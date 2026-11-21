using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test.Projects;

public class MastersNotificationServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeNotificationService notificationService = new();
    private static readonly UserIdentification initiator = new(2);

    private MastersNotificationService CreateService(IUserRepository? userRepository = null)
        => new(notificationService, userRepository ?? new FakeUserRepository());

    private void AssertSingleLetter(NotificationEvent letter)
    {
        letter.NotificationClass.ShouldBe(NotificationClass.MasterProject);
        letter.EntityReference.ShouldBe(new ProjectIdentification(mock.Project.ProjectId));
        letter.TemplateText.TemplateContents.ShouldStartWith("Добрый день, %recepient.name%!");
    }

    [Fact]
    public async Task NotifyMastersWithPermission_SendsOnlyToMastersWithPermission()
    {
        var extraMaster = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(a => a.UserId == extraMaster.UserId).CanManageMoney = false;
        mock.ReInitProjectInfo();

        await CreateService().NotifyMastersWithPermission(mock.ProjectInfo, Permission.CanManageMoney, "Тема", "Текст письма", initiator);

        var letter = notificationService.Queued.ShouldHaveSingleItem();
        AssertSingleLetter(letter);
        letter.Recepients.Select(r => r.UserId).ShouldBe([new UserIdentification(mock.Master.UserId)]);
    }

    [Fact]
    public async Task NotifyAllMasters_SendsToEveryMaster()
    {
        var extraMaster = mock.CreateMaster();
        mock.ReInitProjectInfo();

        await CreateService().NotifyAllMasters(mock.ProjectInfo, "Тема", "Текст письма", initiator);

        var letter = notificationService.Queued.ShouldHaveSingleItem();
        AssertSingleLetter(letter);
        letter.Recepients.Select(r => r.UserId).ShouldBe(
            [new UserIdentification(mock.Master.UserId), new UserIdentification(extraMaster.UserId)],
            ignoreOrder: true);
    }

    [Fact]
    public async Task NotifyMastersAndAdmins_DedupsAdminWhoIsMaster()
    {
        var extraMaster = mock.CreateMaster();
        mock.ReInitProjectInfo();

        // Главный мастер — одновременно админ сайта: он должен получить одно уведомление — как мастер.
        var admins = new[] { mock.MasterInfo.ToUserInfoHeader(), new UserInfoHeader(new UserIdentification(100500), new UserDisplayName("Админ", null), Avatar: null) };

        await CreateService(FakeUserRepository.WithAdmins(admins))
            .NotifyMastersAndAdmins(mock.ProjectInfo, "Тема", "Текст письма", initiator);

        var letter = notificationService.Queued.ShouldHaveSingleItem();
        AssertSingleLetter(letter);
        letter.Recepients.Select(r => r.UserId).ShouldBe(
            [new UserIdentification(mock.Master.UserId), new UserIdentification(extraMaster.UserId), new UserIdentification(100500)],
            ignoreOrder: true);
        letter.Recepients.Single(r => r.UserId == new UserIdentification(mock.Master.UserId))
            .SubscriptionReason.ShouldBe(SubscriptionReason.MasterOfGame);
    }
}
