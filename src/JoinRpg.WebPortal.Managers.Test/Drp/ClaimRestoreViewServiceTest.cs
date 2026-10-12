using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Web.Claims;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using JoinRpg.WebPortal.Managers.Claims;
using JoinRpg.WebPortal.Managers.Drp;
using Microsoft.Extensions.Options;

namespace JoinRpg.WebPortal.Managers.Test.Drp;

public class ClaimRestoreViewServiceTest
{
    private static readonly DateTimeOffset BackupTakenAt = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);

    private readonly MockedProject mock = new();
    private readonly FakeClaimRestoreService restoreService = new();
    private readonly FakeNotificationRepository notifications = new();

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private ClaimRestoreViewService CreateService(int userId) => new(
        new FakeProjectMetadataRepository(mock),
        new FakeCurrentUserAccessor(userId),
        notifications,
        new FakeCharacterInfoRepository(mock),
        new FakeUserRepository(mock),
        new FakeUserLinkResolver(),
        restoreService,
        Options.Create(new DrpOptions { BackupTakenAt = BackupTakenAt, LostAt = LostAt }));

    private void BlockProject()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
    }

    [Fact]
    public async Task GetPlan_ProjectNotBlocked_ExplainsWithoutReadingNotifications()
    {
        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        model.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.ProjectNotBlocked);
        model.Rows.ShouldBeEmpty();
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlan_NotMaster_NoAccess()
    {
        BlockProject();

        var model = await CreateService(mock.Player.UserId).GetPlan(ProjectId);

        model.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.NoAccess);
        model.Rows.ShouldBeEmpty();
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlan_Blocked_ReadsNotificationsBeforeLoss()
    {
        BlockProject();

        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        model.UnavailableReason.ShouldBeNull();
        model.Rows.ShouldBeEmpty();
        model.Masters.ShouldContain(m => m.UserId == new UserIdentification(mock.Master.UserId));
        notifications.GetClaimNotificationsForProjectCalls.ShouldHaveSingleItem().ShouldBe((ProjectId, LostAt));
    }

    [Fact]
    public async Task Apply_Create_RestoresLostClaimWithTargetState()
    {
        var claimId = new ClaimIdentification(ProjectId, 999);
        var character = new CharacterIdentification(ProjectId, mock.Character.CharacterId);
        var createdAt = BackupTakenAt.AddDays(3);
        var statusChangedAt = BackupTakenAt.AddDays(4);
        var player = new UserIdentification(mock.Player.UserId);
        var master = new UserIdentification(mock.Master.UserId);

        var result = await CreateService(mock.Master.UserId).Apply(new ClaimRestoreApplyRequest(
            claimId, ClaimRestoreActionView.Create, character, NewCharacterName: null, player, PlayerLink: null,
            ClaimStatusView.Approved, statusChangedAt, master, 700, createdAt));

        result.Character.ShouldBe(character);
        var lost = restoreService.Lost.ShouldHaveSingleItem();
        lost.ShouldBe(new LostClaimToRestore(
            claimId, character, null, player, master, createdAt,
            new RestoredClaimTarget(null, ClaimStatus.Approved, statusChangedAt, null, 700)));
    }

    [Fact]
    public async Task Apply_CreateWithPlayerLink_ResolvesPlayer()
    {
        var master = new UserIdentification(mock.Master.UserId);

        _ = await CreateService(mock.Master.UserId).Apply(new ClaimRestoreApplyRequest(
            new ClaimIdentification(ProjectId, 999), ClaimRestoreActionView.Create, null, "Новый", Player: null, PlayerLink: "42",
            null, null, master, null, BackupTakenAt));

        var lost = restoreService.Lost.ShouldHaveSingleItem();
        lost.Player.ShouldBe(new UserIdentification(42));
        lost.NewCharacterName.ShouldBe("Новый");
        lost.Character.ShouldBeNull();
    }

    [Fact]
    public async Task Apply_CreateWithoutPlayer_Refuses()
        => await Should.ThrowAsync<JoinValidationException>(() => CreateService(mock.Master.UserId).Apply(new ClaimRestoreApplyRequest(
            new ClaimIdentification(ProjectId, 999), ClaimRestoreActionView.Create, null, "Новый", null, " ",
            null, null, new UserIdentification(mock.Master.UserId), null, BackupTakenAt)));

    [Fact]
    public async Task Apply_Change_AppliesRestoredState()
    {
        var claimId = new ClaimIdentification(ProjectId, 5);
        var moveTo = new CharacterIdentification(ProjectId, mock.Character.CharacterId);
        var master = new UserIdentification(mock.Master.UserId);
        var statusChangedAt = BackupTakenAt.AddDays(4);

        _ = await CreateService(mock.Master.UserId).Apply(new ClaimRestoreApplyRequest(
            claimId, ClaimRestoreActionView.Change, moveTo, null, null, null,
            ClaimStatusView.DeclinedByMaster, statusChangedAt, master, 300, BackupTakenAt));

        restoreService.Lost.ShouldBeEmpty();
        restoreService.Applied.ShouldHaveSingleItem().ShouldBe(
            (claimId, new RestoredClaimTarget(moveTo, ClaimStatus.DeclinedByMaster, statusChangedAt, master, 300)));
    }

    /// <summary>Модели ездят между сервером и островом по JSON (у мок-проекта id нулевой, поэтому свои id).</summary>
    [Fact]
    public void Models_SurviveJson()
    {
        var projectId = new ProjectIdentification(1);
        var master = new UserIdentification(2);
        var character = new ClaimRestoreCharacterOption(new CharacterIdentification(projectId, 7), "Герой");
        var player = new UserLinkViewModel(new UserIdentification(10), "Игрок", ViewMode.Show);
        var row = new ClaimRestoreRowViewModel(
            new ClaimIdentification(projectId, 999), ClaimRestoreActionView.Change, "Герой", character, character, [character],
            player, [player], ClaimStatusView.AddedByUser, ClaimStatusView.Approved, BackupTakenAt, master, master, 100, 200,
            BackupTakenAt, LostAt, [ClaimRestoreWarningView.CharacterRenamed]);
        var plan = new ClaimRestorePlanViewModel(projectId, null, BackupTakenAt, LostAt, [row], [new ClaimRestoreMasterOption(master, "Мастер")]);

        var restored = JsonRoundTrip.Ensure(plan);
        restored.Rows.Single().ClaimId.ShouldBe(row.ClaimId);
        restored.Rows.Single().Character.ShouldBe(character);
        restored.Rows.Single().Player.ShouldBe(player);
        restored.Masters.Single().UserId.ShouldBe(master);

        var request = new ClaimRestoreApplyRequest(
            row.ClaimId, ClaimRestoreActionView.Create, null, "Новый", null, "42",
            null, null, master, null, BackupTakenAt);
        JsonRoundTrip.Ensure(request).ShouldBe(request);

        var result = new ClaimRestoreApplyResult(character.CharacterId);
        JsonRoundTrip.Ensure(result).ShouldBe(result);
    }

    private sealed class FakeClaimRestoreService : IClaimRestoreService
    {
        public List<(ClaimIdentification ClaimId, RestoredClaimTarget Target)> Applied { get; } = [];
        public List<LostClaimToRestore> Lost { get; } = [];

        public Task ApplyRestoredState(ClaimIdentification claimId, RestoredClaimTarget target)
        {
            Applied.Add((claimId, target));
            return Task.CompletedTask;
        }

        public Task<CharacterIdentification> RestoreLostClaim(LostClaimToRestore claim)
        {
            Lost.Add(claim);
            return Task.FromResult(claim.Character ?? new CharacterIdentification(claim.ClaimId.ProjectId, 12345));
        }
    }

    private sealed class FakeUserLinkResolver : IUserLinkResolver
    {
        public Task<UserIdentification> ResolveAsync(string userLink) => Task.FromResult(new UserIdentification(int.Parse(userLink)));
    }

}
