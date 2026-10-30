using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Email;

namespace JoinRpg.Services.Impl.Test;

public class MassProjectEmailServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeNotificationService notificationService = new();

    private ClaimWithPlayer Claim => new()
    {
        ClaimId = new ClaimIdentification(mock.Project.ProjectId, 1),
        CharacterId = new CharacterIdentification(mock.Project.ProjectId, 1),
        CharacterName = "CharacterName",
        ExtraNicknames = "",
        Player = new UserInfoHeader(new UserIdentification(mock.Player.UserId), new UserDisplayName("Player", null), Avatar: null),
        ResponsibleMasterUserId = new UserIdentification(mock.Master.UserId),
    };

    private MassProjectEmailService CreateService()
        => new(
            new FakeClaimsRepository(mock) { Headers = { Claim } },
            new FakeProjectMetadataRepository(mock),
            notificationService,
            new FakeCurrentUserAccessor(mock.Master.UserId),
            new FakeClaimUriLocator());

    [Fact]
    public async Task MassMail_HeaderStartsWithProjectName()
    {
        await CreateService().MassMail(
            [Claim.ClaimId],
            new MarkdownDbValue("Текст рассылки"),
            "Тема рассылки",
            alsoMailToMasters: false);

        notificationService.Queued.Single().Header.ShouldBe("Mocked project: Тема рассылки");
    }

    [Fact]
    public async Task MassMail_ToMasters_HeaderStartsWithProjectName()
    {
        await CreateService().MassMail(
            [Claim.ClaimId],
            new MarkdownDbValue("Текст рассылки"),
            "Тема рассылки",
            alsoMailToMasters: true);

        notificationService.Queued.Select(n => n.Header)
            .ShouldAllBe(header => header == "Mocked project: Тема рассылки");
    }

    /// <summary>Ссылка под письмом: игроку — на его заявку, мастерам — на проект (#5752).</summary>
    [Fact]
    public async Task MassMail_EntityReference_MyClaimForPlayers_ProjectForMasters()
    {
        await CreateService().MassMail(
            [Claim.ClaimId],
            new MarkdownDbValue("Текст рассылки"),
            "Тема рассылки",
            alsoMailToMasters: true);

        var projectId = new ProjectIdentification(mock.Project.ProjectId);
        notificationService.Queued.Single(n => n.NotificationClass == NotificationClass.MassProjectEmails)
            .EntityReference.ShouldBe(new MyClaimInProjectIdentification(projectId));
        notificationService.Queued.Single(n => n.NotificationClass == NotificationClass.MasterProject)
            .EntityReference.ShouldBe(projectId);
    }

    private sealed class FakeClaimUriLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new($"https://joinrpg.ru/{target.ProjectId.Value}/claim/{target.ClaimId}");
    }
}
