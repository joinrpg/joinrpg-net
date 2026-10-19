using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Web.Claims;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using JoinRpg.WebPortal.Managers.Drp;

namespace JoinRpg.WebPortal.Managers.Test.Drp;

public class ClaimRestorePlanBuilderTest
{
    private static readonly DateTimeOffset BackupTakenAt = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);

    private static DrpOptions Options => new() { BackupTakenAt = BackupTakenAt, LostAt = LostAt };

    private readonly MockedProject mock = new();

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private UserIdentification PlayerId => new(mock.Player.UserId);

    private UserIdentification MasterId => new(mock.Master.UserId);

    private ClaimRestoreSource BuildSource()
        => ClaimRestorePlanBuilder.BuildSource(
            mock.ProjectInfo,
            [],
            [.. mock.Project.Characters.Select(mock.GetCharacterInfo)],
            BackupTakenAt);

    [Fact]
    public void BuildSource_TakesAllCharactersAndTheirClaims()
    {
        var slot = mock.CreateSlot("Слот");
        var deleted = mock.CreateCharacter("Удалённый");
        deleted.IsActive = false;
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.CurrentFee = 1500;

        var source = BuildSource();

        source.Characters.Single(c => c.CharacterId.CharacterId == slot.CharacterId).IsSlot.ShouldBeTrue();
        source.Characters.Single(c => c.CharacterId.CharacterId == mock.Character.CharacterId).IsSlot.ShouldBeFalse();
        source.Characters.Single(c => c.CharacterId.CharacterId == deleted.CharacterId).IsActive.ShouldBeFalse();

        var existing = source.Claims.ShouldHaveSingleItem();
        existing.ClaimId.ShouldBe(claim.GetId());
        existing.CharacterId.CharacterId.ShouldBe(mock.Character.CharacterId);
        existing.Player.ShouldBe(PlayerId);
        existing.ResponsibleMaster.ShouldBe(MasterId);
        existing.Status.ShouldBe(ClaimStatus.AddedByUser);
        existing.ManualFee.ShouldBe(1500);
        existing.FeePaid.ShouldBe(0);

        source.DefaultResponsibleMaster.ShouldBe(MasterId);
        source.Masters.ShouldContain(m => m.UserId == MasterId);
        source.BackupTakenAt.ShouldBe(BackupTakenAt);
    }

    [Fact]
    public void BuildSource_FeePaidIsSumOfApprovedOperations()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.FinanceOperations.Add(new FinanceOperation { MoneyAmount = 1000, State = FinanceOperationState.Approved, OperationType = FinanceOperationType.Submit });
        claim.FinanceOperations.Add(new FinanceOperation { MoneyAmount = 300, State = FinanceOperationState.Approved, OperationType = FinanceOperationType.Online });
        claim.FinanceOperations.Add(new FinanceOperation { MoneyAmount = 700, State = FinanceOperationState.Proposed, OperationType = FinanceOperationType.Submit });

        BuildSource().Claims.ShouldHaveSingleItem().FeePaid.ShouldBe(1300);
    }

    [Fact]
    public void Build_FeePaidMismatch_KeepsBothSums()
    {
        var row = new ClaimRestorePlanRow(
            new ClaimIdentification(ProjectId, 999),
            ClaimRestoreAction.Nothing,
            null, null, [], null, [], null, null, null, null, BackupTakenAt, BackupTakenAt,
            [ClaimRestoreWarning.FeePaidMismatch])
        {
            FeePaidMismatch = new FeePaidMismatch(Exported: 1500, InDatabase: 1000),
        };
        var matched = row with { ClaimId = new ClaimIdentification(ProjectId, 998), Warnings = [], FeePaidMismatch = null };

        var model = ClaimRestorePlanBuilder.Build(mock.ProjectInfo, BuildSource(), [row, matched], [], Options);

        model.Rows[0].Warnings.ShouldBe([ClaimRestoreWarningView.FeePaidMismatch]);
        model.Rows[0].FeePaidMismatch.ShouldBe(new ClaimRestoreFeePaidMismatchView(Exported: 1500, InDatabase: 1000));
        model.Rows[1].FeePaidMismatch.ShouldBeNull();
    }

    [Fact]
    public void Build_ChangeRow_ShowsCurrentStateNextToTarget()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var other = mock.CreateCharacter("Другой");
        var source = BuildSource();
        var statusChangedAt = BackupTakenAt.AddDays(10);

        var row = new ClaimRestorePlanRow(
            claim.GetId(),
            ClaimRestoreAction.Change,
            CharacterName: "Другой",
            Character: new CharacterIdentification(ProjectId, other.CharacterId),
            CharacterCandidates: [],
            Player: PlayerId,
            PlayerCandidates: [PlayerId],
            Status: ClaimStatus.Approved,
            StatusChangedAt: statusChangedAt,
            ResponsibleMaster: null,
            ManualFee: 500,
            FirstActivity: BackupTakenAt.AddDays(1),
            LastActivity: statusChangedAt,
            Warnings: [ClaimRestoreWarning.ResponsibleNotFound]);

        var model = ClaimRestorePlanBuilder.Build(mock.ProjectInfo, source, [row], [new UserInfoHeader(PlayerId, mock.PlayerInfo.DisplayName)], Options);

        model.UnavailableReason.ShouldBeNull();
        model.BackupTakenAt.ShouldBe(BackupTakenAt);
        model.LostAt.ShouldBe(LostAt);
        model.Masters.ShouldContain(m => m.UserId == MasterId);

        var result = model.Rows.ShouldHaveSingleItem();
        result.Action.ShouldBe(ClaimRestoreActionView.Change);
        result.CurrentCharacter.ShouldBe(new ClaimRestoreCharacterOption(new CharacterIdentification(ProjectId, mock.Character.CharacterId), "Some Character"));
        result.Character.ShouldBe(new ClaimRestoreCharacterOption(new CharacterIdentification(ProjectId, other.CharacterId), "Другой"));
        result.CurrentStatus.ShouldBe(ClaimStatusView.AddedByUser);
        result.Status.ShouldBe(ClaimStatusView.Approved);
        result.StatusChangedAt.ShouldBe(statusChangedAt);
        result.CurrentResponsibleMaster.ShouldBe(MasterId);
        result.ResponsibleMaster.ShouldBeNull();
        result.ManualFee.ShouldBe(500);
        result.Player.ShouldNotBeNull().DisplayName.ShouldBe(mock.PlayerInfo.DisplayName.DisplayName);
        result.Warnings.ShouldBe([ClaimRestoreWarningView.ResponsibleNotFound]);
    }

    [Fact]
    public void Build_CreateRow_ResolvesCandidatesAndKeepsUnknownUsersById()
    {
        var source = BuildSource();
        var unknown = new UserIdentification(777);
        var row = new ClaimRestorePlanRow(
            new ClaimIdentification(ProjectId, 999),
            ClaimRestoreAction.Create,
            CharacterName: "Новый",
            Character: null,
            CharacterCandidates: [new CharacterIdentification(ProjectId, mock.Character.CharacterId)],
            Player: null,
            PlayerCandidates: [PlayerId, unknown],
            Status: null,
            StatusChangedAt: null,
            ResponsibleMaster: MasterId,
            ManualFee: null,
            FirstActivity: BackupTakenAt.AddDays(1),
            LastActivity: BackupTakenAt.AddDays(2),
            Warnings: [ClaimRestoreWarning.PlayerAmbiguous, ClaimRestoreWarning.StatusUnknown]);

        var plan = new[] { row };
        ClaimRestorePlanBuilder.UsersToShow(plan).ShouldBe([PlayerId, unknown], ignoreOrder: true);

        var model = ClaimRestorePlanBuilder.Build(mock.ProjectInfo, source, plan, [new UserInfoHeader(PlayerId, mock.PlayerInfo.DisplayName)], Options);

        var result = model.Rows.ShouldHaveSingleItem();
        result.Action.ShouldBe(ClaimRestoreActionView.Create);
        result.CurrentCharacter.ShouldBeNull();
        result.CurrentStatus.ShouldBeNull();
        result.Character.ShouldBeNull();
        result.CharacterCandidates.ShouldBe([new ClaimRestoreCharacterOption(new CharacterIdentification(ProjectId, mock.Character.CharacterId), "Some Character")]);
        result.Player.ShouldBeNull();
        result.PlayerCandidates.Select(p => p.UserId).ShouldBe([PlayerId, unknown]);
        result.PlayerCandidates[1].DisplayName.ShouldBe("777");
        result.ResponsibleMaster.ShouldBe(MasterId);
        result.Warnings.ShouldBe([ClaimRestoreWarningView.PlayerAmbiguous, ClaimRestoreWarningView.StatusUnknown]);
    }

    [Fact]
    public void BuildSource_CharacterFromSlotKnowsItsSlot()
    {
        var slot = mock.CreateSlot("Стражник");
        var child = mock.CreateCharacter("Стражник");
        child.OriginalCharacterSlot = slot;

        var source = BuildSource();

        source.Characters.Single(c => c.CharacterId.CharacterId == child.CharacterId).OriginalSlotId
            .ShouldBe(new CharacterIdentification(ProjectId, slot.CharacterId));
        source.Characters.Single(c => c.CharacterId.CharacterId == slot.CharacterId).OriginalSlotId.ShouldBeNull();
    }

    [Fact]
    public void Build_NewCharacterIsSlot_Passed()
    {
        var row = new ClaimRestorePlanRow(
            new ClaimIdentification(ProjectId, 999),
            ClaimRestoreAction.Create,
            "Стражник", null, [], PlayerId, [PlayerId], ClaimStatus.Approved, BackupTakenAt, MasterId, null, BackupTakenAt, BackupTakenAt,
            [ClaimRestoreWarning.LooksLikeSlot])
        {
            NewCharacterIsSlot = true,
        };

        var result = ClaimRestorePlanBuilder.Build(mock.ProjectInfo, BuildSource(), [row], [], Options).Rows.Single();

        result.NewCharacterIsSlot.ShouldBeTrue();
        result.Warnings.ShouldBe([ClaimRestoreWarningView.LooksLikeSlot]);
    }

    [Fact]
    public void Build_MapsEveryWarning()
    {
        var row = new ClaimRestorePlanRow(
            new ClaimIdentification(ProjectId, 999),
            ClaimRestoreAction.Nothing,
            null, null, [], null, [], null, null, null, null, BackupTakenAt, BackupTakenAt,
            Enum.GetValues<ClaimRestoreWarning>());

        var model = ClaimRestorePlanBuilder.Build(mock.ProjectInfo, BuildSource(), [row], [], Options);

        model.Rows.Single().Warnings.Select(w => w.ToString())
            .ShouldBe(Enum.GetValues<ClaimRestoreWarning>().Select(w => w.ToString()));
    }

    [Fact]
    public void Build_ResponsibleNotAmongMasters_LeftForMasterToChoose()
    {
        var formerMaster = new UserIdentification(555);
        var create = new ClaimRestorePlanRow(
            new ClaimIdentification(ProjectId, 999), ClaimRestoreAction.Create,
            "Новый", null, [], PlayerId, [PlayerId], null, null, formerMaster, null, BackupTakenAt, BackupTakenAt, []);
        var change = create with { ClaimId = new ClaimIdentification(ProjectId, 998), Action = ClaimRestoreAction.Change };
        var known = create with { ClaimId = new ClaimIdentification(ProjectId, 997), ResponsibleMaster = MasterId };

        var model = ClaimRestorePlanBuilder.Build(mock.ProjectInfo, BuildSource(), [create, change, known], [], Options);

        model.Rows[0].ResponsibleMaster.ShouldBeNull();
        model.Rows[1].ResponsibleMaster.ShouldBeNull();
        model.Rows[2].ResponsibleMaster.ShouldBe(MasterId);
    }

    /// <summary>Статусы переводятся приведением — перечисления должны совпадать по составу и порядку.</summary>
    [Fact]
    public void ClaimStatusView_MatchesDomainStatus()
    {
        foreach (var status in Enum.GetValues<ClaimStatus>())
        {
            ((ClaimStatusView)status).ToString().ShouldBe(status.ToString());
        }
        Enum.GetValues<ClaimStatusView>().Length.ShouldBe(Enum.GetValues<ClaimStatus>().Length);
    }
}
