using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Drp.MasterExports;
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
    private readonly FakeExportRestoreService exportRestoreService = new();
    private readonly FakeLookupRepository lookup = new();

    private Task<ClaimRestorePlanViewModel> GetPlan(int userId, params MasterExportForPlan[] exports)
        => CreateService(userId).GetPlan(new ClaimRestorePlanRequest(ProjectId, exports));

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private ClaimRestoreViewService CreateService(int userId) => new(
        new FakeProjectMetadataRepository(mock),
        new FakeCurrentUserAccessor(userId),
        notifications,
        new FakeCharacterInfoRepository(mock),
        new FakeUserRepository(mock),
        new FakeUserLinkResolver(),
        restoreService,
        exportRestoreService,
        lookup,
        Options.Create(new DrpOptions { BackupTakenAt = BackupTakenAt, LostAt = LostAt }));

    private void BlockProject()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
    }

    [Fact]
    public async Task GetPlan_ProjectNotBlocked_ExplainsWithoutReadingNotifications()
    {
        var model = await GetPlan(mock.Master.UserId);

        model.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.ProjectNotBlocked);
        model.Rows.ShouldBeEmpty();
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlan_NotMaster_NoAccess()
    {
        BlockProject();

        var model = await GetPlan(mock.Player.UserId);

        model.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.NoAccess);
        model.Rows.ShouldBeEmpty();
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlan_Blocked_ReadsNotificationsBeforeLoss()
    {
        BlockProject();

        var model = await GetPlan(mock.Master.UserId);

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

    private ParsedMasterExport CharacterExport(int characterId, int groupId)
        => new(
            MasterExportKind.Characters,
            ProjectId.Value,
            SuggestedDate: null,
            OtherHeaders: [],
            Rows: [new MasterExportRow(
                RowNumber: 2,
                CharacterId: characterId,
                ClaimId: null,
                Name: "Потерянный",
                Groups: [new MasterExportGroupRef(groupId, "Потерянная группа")],
                ResponsibleMasterName: null,
                PlayerEmail: "PLAYER@example.com",
                PlayerDisplayName: null,
                Status: null,
                TotalFee: null,
                FeePaid: null,
                ClaimCreatedAt: null,
                ClaimUpdatedAt: null,
                OtherCells: [])],
            Warnings: []);

    [Fact]
    public async Task GetPlan_WithExport_LooksUpIdsAndEmailsAndPlansCreation()
    {
        BlockProject();
        var payload = MasterExportPayload.Serialize(CharacterExport(9999, 8888));

        var model = await GetPlan(mock.Master.UserId, new MasterExportForPlan(payload, LostAt.AddDays(-1)));

        model.UnavailableReason.ShouldBeNull();
        model.CanEditRoles.ShouldBeTrue();
        lookup.Emails.ShouldBe(["PLAYER@example.com"]);
        lookup.GroupIds.ShouldBe([new CharacterGroupIdentification(ProjectId, 8888)]);
        lookup.CharacterIds.ShouldBe([new CharacterIdentification(ProjectId, 9999)]);
        var group = model.Groups.ShouldHaveSingleItem();
        group.Action.ShouldBe(ClaimRestoreActionView.Create);
        group.Name.ShouldBe("Потерянная группа");
        var creation = model.CharacterCreations.ShouldHaveSingleItem();
        creation.CharacterId.ShouldBe(new CharacterIdentification(ProjectId, 9999));
        creation.Groups.ShouldNotBeNull().ShouldHaveSingleItem().Name.ShouldBe("Потерянная группа");
    }

    [Fact]
    public async Task GetPlan_WithoutExports_DoesNotLookUpAnything()
    {
        BlockProject();

        _ = await GetPlan(mock.Master.UserId);

        lookup.Emails.ShouldBeEmpty();
        lookup.GroupIds.ShouldBeEmpty();
        lookup.CharacterIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlan_GroupIdTakenByOtherProject_NotCreated()
    {
        BlockProject();
        lookup.TakenGroups.Add(8888);
        var payload = MasterExportPayload.Serialize(CharacterExport(9999, 8888));

        var model = await GetPlan(mock.Master.UserId, new MasterExportForPlan(payload, LostAt.AddDays(-1)));

        var group = model.Groups.ShouldHaveSingleItem();
        group.Action.ShouldBe(ClaimRestoreActionView.Nothing);
        group.Warnings.ShouldBe([ProjectRestoreWarningView.GroupIdTakenByOtherProject]);
    }

    [Fact]
    public async Task GetPlan_BrokenPayload_FormatException()
    {
        BlockProject();

        _ = await Should.ThrowAsync<FormatException>(() => GetPlan(mock.Master.UserId, new MasterExportForPlan("не выгрузка", LostAt)));
    }

    [Fact]
    public async Task ParseExport_NotAnExport_ReturnsReason()
    {
        using var content = new MemoryStream("просто текст"u8.ToArray());

        var result = await CreateService(mock.Master.UserId).ParseExport(ProjectId, "notes.xlsx", content);

        result.Export.ShouldBeNull();
        result.Error.ShouldNotBeNull();
    }

    [Fact]
    public async Task ApplyGroup_RestoresLostGroup()
    {
        var groupId = new CharacterGroupIdentification(ProjectId, 8888);

        await CreateService(mock.Master.UserId).ApplyGroup(new GroupRestoreApplyRequest(groupId, " Группа "));

        exportRestoreService.Groups.ShouldHaveSingleItem().ShouldBe((groupId, "Группа"));
    }

    [Fact]
    public async Task ApplyCharacter_CreateAndChange()
    {
        var characterId = new CharacterIdentification(ProjectId, 9999);
        var groupId = new CharacterGroupIdentification(ProjectId, 8888);
        var field = new ProjectFieldIdentification(ProjectId, 5);
        var service = CreateService(mock.Master.UserId);

        await service.ApplyCharacter(new CharacterRestoreApplyRequest(characterId, true, "Герой", [groupId], [new RestoreFieldValueRequest(field, "x")]));
        await service.ApplyCharacter(new CharacterRestoreApplyRequest(characterId, false, null, null, [new RestoreFieldValueRequest(field, null)]));

        var created = exportRestoreService.Created.ShouldHaveSingleItem();
        created.Name.ShouldBe("Герой");
        created.Target.Groups.ShouldBe([groupId]);
        created.Target.Fields.ShouldNotBeNull()[field].ShouldBe("x");
        var changed = exportRestoreService.Changed.ShouldHaveSingleItem();
        changed.Target.Groups.ShouldBeNull();
        changed.Target.Fields.ShouldNotBeNull()[field].ShouldBeNull();
    }

    [Fact]
    public async Task ApplyCharacter_CreateWithoutName_Refuses()
        => await Should.ThrowAsync<JoinValidationException>(() => CreateService(mock.Master.UserId).ApplyCharacter(
            new CharacterRestoreApplyRequest(new CharacterIdentification(ProjectId, 9999), true, " ", null, [])));

    [Fact]
    public async Task ApplyClaimFields_PassesDelta()
    {
        var claimId = new ClaimIdentification(ProjectId, 5);
        var field = new ProjectFieldIdentification(ProjectId, 5);

        await CreateService(mock.Master.UserId).ApplyClaimFields(new ClaimFieldsRestoreApplyRequest(claimId, [new RestoreFieldValueRequest(field, "y")]));

        var applied = exportRestoreService.ClaimFields.ShouldHaveSingleItem();
        applied.ClaimId.ShouldBe(claimId);
        applied.Fields[field].ShouldBe("y");
    }

    private sealed class FakeExportRestoreService : IExportRestoreService
    {
        public List<(CharacterGroupIdentification GroupId, string Name)> Groups { get; } = [];
        public List<(CharacterIdentification CharacterId, string Name, RestoredCharacterTarget Target)> Created { get; } = [];
        public List<(CharacterIdentification CharacterId, RestoredCharacterTarget Target)> Changed { get; } = [];
        public List<(ClaimIdentification ClaimId, IReadOnlyDictionary<ProjectFieldIdentification, string?> Fields)> ClaimFields { get; } = [];

        public Task ApplyRestoredCharacter(CharacterIdentification characterId, RestoredCharacterTarget target)
        {
            Changed.Add((characterId, target));
            return Task.CompletedTask;
        }

        public Task ApplyRestoredClaimFields(ClaimIdentification claimId, IReadOnlyDictionary<ProjectFieldIdentification, string?> fields)
        {
            ClaimFields.Add((claimId, fields));
            return Task.CompletedTask;
        }

        public Task RestoreLostGroup(CharacterGroupIdentification groupId, string name)
        {
            Groups.Add((groupId, name));
            return Task.CompletedTask;
        }

        public Task RestoreLostCharacter(CharacterIdentification characterId, string name, RestoredCharacterTarget target)
        {
            Created.Add((characterId, name, target));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLookupRepository : IProjectRestoreLookupRepository
    {
        public List<string> Emails { get; } = [];
        public List<CharacterGroupIdentification> GroupIds { get; } = [];
        public List<CharacterIdentification> CharacterIds { get; } = [];
        public HashSet<int> TakenGroups { get; } = [];

        public Task<IReadOnlyCollection<CharacterGroupIdentification>> GetGroupsTakenByOtherProjects(IReadOnlyCollection<CharacterGroupIdentification> groupIds)
        {
            GroupIds.AddRange(groupIds);
            return Task.FromResult<IReadOnlyCollection<CharacterGroupIdentification>>([.. groupIds.Where(g => TakenGroups.Contains(g.CharacterGroupId))]);
        }

        public Task<IReadOnlyCollection<CharacterIdentification>> GetCharactersTakenByOtherProjects(IReadOnlyCollection<CharacterIdentification> characterIds)
        {
            CharacterIds.AddRange(characterIds);
            return Task.FromResult<IReadOnlyCollection<CharacterIdentification>>([]);
        }

        public Task<IReadOnlyDictionary<string, UserIdentification>> FindUsersByEmails(IReadOnlyCollection<string> emails)
        {
            Emails.AddRange(emails);
            return Task.FromResult<IReadOnlyDictionary<string, UserIdentification>>(new Dictionary<string, UserIdentification>());
        }

        public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
            => throw new NotSupportedException();

        public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(ClaimIdentification claimId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
            => throw new NotSupportedException();
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

        public Task<RestoredCommentsResult> RestoreClaimComments(ClaimIdentification claimId, IReadOnlyList<RestoredClaimComment> comments)
            => throw new NotSupportedException();
    }

    private sealed class FakeUserLinkResolver : IUserLinkResolver
    {
        public Task<UserIdentification> ResolveAsync(string userLink) => Task.FromResult(new UserIdentification(int.Parse(userLink)));
    }
}
