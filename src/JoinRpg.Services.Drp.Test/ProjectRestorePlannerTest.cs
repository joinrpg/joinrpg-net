using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Drp.MasterExports;
using static JoinRpg.Services.Drp.Test.ClaimNotificationFactory;

namespace JoinRpg.Services.Drp.Test;

public class ProjectRestorePlannerTest
{
    private static readonly DateTimeOffset BackupAt = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LostAt = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AfterRestore = LostAt.AddDays(3);
    private const string PlayerEmail = "player@example.com";

    private readonly MockedProject mock = new();
    private readonly ProjectFieldInfo strength;
    private readonly ProjectFieldInfo wishes;
    private readonly ProjectFieldInfo agility;

    public ProjectRestorePlannerTest()
    {
        agility = mock.AddField(f =>
        {
            f.FieldName = "Ловкость";
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = FieldBoundTo.Character;
        });
        strength = mock.AddField(f =>
        {
            f.FieldName = "Сила";
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = FieldBoundTo.Character;
        });
        wishes = mock.AddField(f =>
        {
            f.FieldName = "Пожелания";
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = FieldBoundTo.Claim;
        });
    }

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private CharacterIdentification CharacterId(int id) => new(ProjectId, id);
    private CharacterGroupIdentification GroupId(int id) => new(ProjectId, id);
    private ClaimIdentification ClaimId(int id) => new(ProjectId, id);

    private static MasterExportRow CharacterRow(int id, string name, (int Id, string Name)[] groups, string strength = "", string? playerEmail = null)
        => new(id, id, null, name, [.. groups.Select(g => new MasterExportGroupRef(g.Id, g.Name))], Master.DisplayName.DisplayName,
            playerEmail, null, null, null, null, null, null, [new MasterExportCell("Сила", strength, false)]);

    private static MasterExportRow ClaimRow(int id, string characterName, ClaimStatus status, string? playerEmail = PlayerEmail, string wishes = "", string strength = "", int? totalFee = null, int? feePaid = null)
        => new(id, null, id, characterName, [], Master.DisplayName.DisplayName, playerEmail, null, status, totalFee, feePaid, null, null,
            [new MasterExportCell("Пожелания", wishes, false), new MasterExportCell("Сила", strength, false)]);

    private DatedMasterExport CharacterExport(DateTimeOffset at, params MasterExportRow[] rows)
        => new(new ParsedMasterExport(MasterExportKind.Characters, ProjectId.Value, null, ["Сила"], rows, []), at);

    private DatedMasterExport ClaimExport(DateTimeOffset at, params MasterExportRow[] rows)
        => new(new ParsedMasterExport(MasterExportKind.Claims, ProjectId.Value, null, ["Пожелания", "Сила"], rows, []), at);

    /// <summary>Слой полей в метаданных мок-проекта — так его строит вызывающий из JSON сущности.</summary>
    private FieldLayerContainer Layer(params (ProjectFieldInfo Field, string? Value)[] values)
        => new(mock.ProjectInfo, values.ToDictionary(v => v.Field.Id.ProjectFieldId, v => v.Value));

    private ExistingCharacterInfo DbCharacter(int id, string name, int[]? groups = null, string? strengthValue = null, DateTimeOffset? updatedAt = null)
        => new(CharacterId(id), name, IsActive: true, IsSlot: false, OtherMaster.UserId)
        {
            Groups = [.. (groups ?? []).Select(GroupId)],
            FieldLayer = Layer((strength, strengthValue)),
            UpdatedAt = updatedAt ?? BackupAt.AddDays(-1),
        };

    private ProjectRestorePlan Plan(
        IEnumerable<DatedMasterExport> exports,
        IEnumerable<ExistingCharacterInfo>? characters = null,
        IEnumerable<ExistingClaimInfo>? claims = null,
        IEnumerable<ExistingGroupInfo>? groups = null,
        IEnumerable<IEnumerable<NotificationMessageForRecipient>>? notifications = null,
        IReadOnlyCollection<int>? groupIdsTakenElsewhere = null)
        => ProjectRestorePlanner.Plan(new ProjectRestoreSource(
            mock.ProjectInfo,
            new ClaimRestoreSource(
                ClaimNotificationParser.Parse((notifications ?? []).SelectMany(n => n)),
                [.. claims ?? []],
                [.. characters ?? []],
                [new ProjectMasterName(Master.UserId, Master.DisplayName.DisplayName), new ProjectMasterName(OtherMaster.UserId, OtherMaster.DisplayName.DisplayName)],
                Master.UserId,
                BackupAt),
            [.. exports],
            [.. groups ?? []],
            new Dictionary<string, UserIdentification> { ["Player@Example.com"] = Player.UserId },
            LostAt)
        {
            GroupIdsTakenByOtherProjects = groupIdsTakenElsewhere ?? [],
        });

    [Fact]
    public void OnlyNotificationsGiveSameClaimsAsClaimPlanner()
    {
        var plan = Plan([], notifications: [
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Гамлет", claimId: ClaimId(2000))]);

        plan.Groups.ShouldBeEmpty();
        plan.Characters.ShouldBeEmpty();
        plan.ClaimFields.ShouldBeEmpty();
        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Create);
        row.CharacterName.ShouldBe("Гамлет");
        row.Character.ShouldBeNull();
    }

    [Fact]
    public void CharacterExportCreatesGroupsAndCharactersWithFormerIds()
    {
        var plan = Plan([CharacterExport(Day(5),
            CharacterRow(101, "Гамлет", [(11, "Дания")], strength: "10"),
            CharacterRow(102, "Офелия", [(11, "Дания"), (12, "Двор")]))]);

        plan.Groups.Select(g => (g.GroupId, g.Action, g.Name)).ShouldBe([
            (GroupId(11), ProjectRestoreAction.Create, "Дания"),
            (GroupId(12), ProjectRestoreAction.Create, "Двор"),
        ]);
        plan.Groups.ShouldAllBe(g => g.ApplyByDefault);

        var hamlet = plan.Characters.Single(c => c.CharacterId == CharacterId(101));
        hamlet.Action.ShouldBe(ProjectRestoreAction.Create);
        hamlet.Name.ShouldBe("Гамлет");
        hamlet.Groups.ShouldBe([GroupId(11)]);
        hamlet.ResponsibleMaster.ShouldBe(Master.UserId);
        hamlet.ApplyByDefault.ShouldBeTrue();
        var field = hamlet.Fields.ShouldHaveSingleItem();
        field.Field.ShouldBe(strength.Id);
        field.Value.ShouldBe("10");
        field.ApplyByDefault.ShouldBeTrue();

        var ophelia = plan.Characters.Single(c => c.CharacterId == CharacterId(102));
        ophelia.Groups.ShouldBe([GroupId(11), GroupId(12)], ignoreOrder: true);
        ophelia.Fields.ShouldBeEmpty();

        plan.Exports.ShouldHaveSingleItem().Columns.ShouldHaveSingleItem().Field.ShouldBe(strength.Id);
        plan.Claims.ShouldBeEmpty();
    }

    [Fact]
    public void LatestCharacterExportWinsAndMissingCharacterIsMarked()
    {
        var plan = Plan([
            CharacterExport(Day(5), CharacterRow(101, "Гамлет", [], strength: "10")),
            CharacterExport(Day(2), CharacterRow(101, "Гамлет", [], strength: "5"), CharacterRow(103, "Лаэрт", [])),
        ]);

        var hamlet = plan.Characters.Single(c => c.CharacterId == CharacterId(101));
        hamlet.ExportedAt.ShouldBe(Day(5));
        hamlet.Fields.ShouldHaveSingleItem().Value.ShouldBe("10");
        hamlet.Warnings.ShouldBeEmpty();

        var laertes = plan.Characters.Single(c => c.CharacterId == CharacterId(103));
        laertes.Action.ShouldBe(ProjectRestoreAction.Create);
        laertes.Warnings.ShouldContain(ProjectRestoreWarning.MissingInNewerExport);

        plan.Exports.Select(e => e.ExportedAt).ShouldBe([Day(2), Day(5)]);
    }

    [Fact]
    public void NotificationAfterClaimExportWins()
    {
        var plan = Plan(
            [ClaimExport(Day(3), ClaimRow(2000, "Гамлет", ClaimStatus.Discussed))],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [new ExistingClaimInfo(ClaimId(2000), CharacterId(101), Player.UserId, ClaimStatus.AddedByUser, Master.UserId, null)],
            notifications: [ClaimChange(Day(4), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(2000))]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Change);
        row.Status.ShouldBe(ClaimStatus.Approved);
        row.StatusChangedAt.ShouldBe(Day(4));
        row.Character.ShouldBeNull();
        row.ResponsibleMaster.ShouldBeNull();

        var status = row.Attributes.ShouldNotBeNull().Status.ShouldNotBeNull();
        status.Chosen.ShouldBe(new RestoreSourcedValue<ClaimStatus>(ClaimStatus.Approved, RestoreSourceKind.Notification, Day(4)));
        status.Alternatives.ShouldBe([
            new RestoreSourcedValue<ClaimStatus>(ClaimStatus.Discussed, RestoreSourceKind.Export, Day(3)),
            new RestoreSourcedValue<ClaimStatus>(ClaimStatus.AddedByUser, RestoreSourceKind.Database, BackupAt),
        ]);
    }

    [Fact]
    public void NotificationBeforeClaimExportLoses()
    {
        var plan = Plan(
            [ClaimExport(Day(3), ClaimRow(2000, "Гамлет", ClaimStatus.Approved))],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [new ExistingClaimInfo(ClaimId(2000), CharacterId(101), Player.UserId, ClaimStatus.Discussed, Master.UserId, null)],
            notifications: [ClaimChange(Day(2), CommentExtraAction.DeclineByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(2000))]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Change);
        row.Status.ShouldBe(ClaimStatus.Approved);
        row.Attributes.ShouldNotBeNull().Status.ShouldNotBeNull().Chosen.Source.ShouldBe(RestoreSourceKind.Export);
    }

    [Fact]
    public void ClaimFiledAfterExportBindsToCharacterFromExport()
    {
        var plan = Plan(
            [CharacterExport(Day(5), CharacterRow(101, "Гамлет", []))],
            notifications: [ClaimChange(Day(6), CommentExtraAction.NewClaim, Player, [Master], characterName: "Гамлет", claimId: ClaimId(2000))]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Create);
        row.Character.ShouldBe(CharacterId(101));
        // Ответственный — из выгрузки персонажей.
        row.ResponsibleMaster.ShouldBe(Master.UserId);
    }

    [Fact]
    public void ClaimOnlyInExportIsCreated()
    {
        var plan = Plan([
            CharacterExport(Day(5), CharacterRow(101, "Гамлет", [])),
            ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, wishes: "Хочу корону", strength: "100", totalFee: 1000)),
        ]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Create);
        row.ClaimId.ShouldBe(ClaimId(3000));
        row.Character.ShouldBe(CharacterId(101));
        row.Player.ShouldBe(Player.UserId);
        row.Status.ShouldBe(ClaimStatus.Approved);
        row.ResponsibleMaster.ShouldBe(Master.UserId);
        // Ручного взноса из «Итого» не ставим.
        row.ManualFee.ShouldBeNull();
        row.Warnings.ShouldBeEmpty();

        // У одобренной заявки поля персонажа — это поля персонажа, в заявку идут только её собственные.
        var fields = plan.ClaimFields.ShouldHaveSingleItem();
        fields.ClaimId.ShouldBe(ClaimId(3000));
        var field = fields.Fields.ShouldHaveSingleItem();
        field.Field.ShouldBe(wishes.Id);
        field.Value.ShouldBe("Хочу корону");
        fields.ApplyByDefault.ShouldBeTrue();
    }

    [Fact]
    public void UnapprovedClaimKeepsPlayerSuggestionsForCharacterFields()
    {
        var plan = Plan([ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.AddedByUser, strength: "100"))]);

        plan.ClaimFields.ShouldHaveSingleItem().Fields.ShouldHaveSingleItem().Field.ShouldBe(strength.Id);
    }

    [Fact]
    public void AmbiguousCharacterNameIsResolvedByPlayer()
    {
        var plan = Plan([
            CharacterExport(Day(5),
                CharacterRow(101, "Гамлет", [], playerEmail: "other@example.com"),
                CharacterRow(104, "Гамлет", [], playerEmail: PlayerEmail)),
            ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved)),
        ]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Character.ShouldBe(CharacterId(104));
        row.Warnings.ShouldNotContain(ClaimRestoreWarning.CharacterAmbiguous);
    }

    [Fact]
    public void RerunAfterApplyPlansNothing()
    {
        var plan = Plan(
            [
                CharacterExport(Day(5), CharacterRow(101, "Гамлет", [(11, "Дания")], strength: "10", playerEmail: PlayerEmail)),
                ClaimExport(Day(5), ClaimRow(2000, "Гамлет", ClaimStatus.Approved, wishes: "Хочу корону")),
            ],
            groups: [new ExistingGroupInfo(GroupId(11), "Дания", IsActive: true)],
            characters: [DbCharacter(101, "Гамлет", groups: [11], strengthValue: "10", updatedAt: AfterRestore)],
            claims: [new ExistingClaimInfo(ClaimId(2000), CharacterId(101), Player.UserId, ClaimStatus.Approved, Master.UserId, null)
            {
                FieldLayer = Layer((wishes, "Хочу корону")),
                LastUpdated = AfterRestore,
            }],
            notifications: [
                ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Гамлет", claimId: ClaimId(2000)),
                ClaimChange(Day(2), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(2000)),
            ]);

        plan.Groups.ShouldHaveSingleItem().Action.ShouldBe(ProjectRestoreAction.Nothing);
        plan.Characters.ShouldHaveSingleItem().Action.ShouldBe(ProjectRestoreAction.Nothing);
        plan.Claims.ShouldHaveSingleItem().Action.ShouldBe(ClaimRestoreAction.Nothing);
        plan.ClaimFields.ShouldBeEmpty();
        plan.Notes.ShouldBeEmpty();
    }

    [Fact]
    public void CharacterChangedAfterLossIsNotAppliedByDefault()
    {
        var plan = Plan(
            [CharacterExport(Day(5), CharacterRow(101, "Гамлет", [], strength: "10"))],
            characters: [DbCharacter(101, "Гамлет", strengthValue: "7", updatedAt: AfterRestore)]);

        var row = plan.Characters.ShouldHaveSingleItem();
        row.Action.ShouldBe(ProjectRestoreAction.Change);
        row.Groups.ShouldBeNull();
        var field = row.Fields.ShouldHaveSingleItem();
        field.Value.ShouldBe("10");
        field.CurrentValue.ShouldBe("7");
        row.ApplyByDefault.ShouldBeFalse();
        row.Warnings.ShouldContain(ProjectRestoreWarning.ChangedAfterRestore);
    }

    [Fact]
    public void CharacterChangedBeforeLossIsApplied()
    {
        var plan = Plan(
            [CharacterExport(Day(5), CharacterRow(101, "Гамлет", [(11, "Дания")], strength: "10"))],
            groups: [new ExistingGroupInfo(GroupId(11), "Дания", IsActive: true)],
            characters: [DbCharacter(101, "Гамлет", strengthValue: "7")]);

        var row = plan.Characters.ShouldHaveSingleItem();
        row.Action.ShouldBe(ProjectRestoreAction.Change);
        row.Groups.ShouldBe([GroupId(11)]);
        row.ApplyByDefault.ShouldBeTrue();
    }

    [Fact]
    public void GroupIdTakenByOtherProjectIsNotCreated()
    {
        var plan = Plan(
            [CharacterExport(Day(5), CharacterRow(102, "Офелия", [(11, "Дания"), (12, "Двор")]))],
            groupIdsTakenElsewhere: [12]);

        var court = plan.Groups.Single(g => g.GroupId == GroupId(12));
        court.Action.ShouldBe(ProjectRestoreAction.Nothing);
        court.ApplyByDefault.ShouldBeFalse();
        court.Warnings.ShouldContain(ProjectRestoreWarning.GroupIdTakenByOtherProject);

        var ophelia = plan.Characters.ShouldHaveSingleItem();
        ophelia.Groups.ShouldBe([GroupId(11)]);
        ophelia.Warnings.ShouldContain(ProjectRestoreWarning.GroupSkipped);
    }

    [Fact]
    public void ExistingGroupWithOtherNameIsNotRenamed()
    {
        var plan = Plan(
            [CharacterExport(Day(5), CharacterRow(101, "Гамлет", [(11, "Дания")]))],
            groups: [new ExistingGroupInfo(GroupId(11), "Датское королевство", IsActive: true)]);

        var group = plan.Groups.ShouldHaveSingleItem();
        group.Action.ShouldBe(ProjectRestoreAction.Nothing);
        group.Warnings.ShouldBe([ProjectRestoreWarning.GroupNameDiffers]);
    }

    [Fact]
    public void PlayerEmailNotFound()
    {
        var plan = Plan([ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, playerEmail: "stranger@example.com"))]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Player.ShouldBeNull();
        row.Warnings.ShouldContain(ClaimRestoreWarning.PlayerNotFound);
    }

    [Fact]
    public void ExportFromOtherProjectIsSkipped()
    {
        var foreign = CharacterExport(Day(5), CharacterRow(101, "Гамлет", []));
        var plan = Plan([foreign with { Export = foreign.Export with { ProjectId = ProjectId.Value + 1 } }]);

        plan.Characters.ShouldBeEmpty();
        plan.Exports.ShouldHaveSingleItem().Used.ShouldBeFalse();
        plan.Notes.ShouldHaveSingleItem().Warning.ShouldBe(ProjectRestoreWarning.ExportFromOtherProject);
    }

    [Fact]
    public void ExportOlderThanBackupDoesNotOverrideDatabaseByDefault()
    {
        var plan = Plan(
            [CharacterExport(BackupAt.AddDays(-10), CharacterRow(101, "Гамлет", [], strength: "10"))],
            characters: [DbCharacter(101, "Гамлет", strengthValue: "7")]);

        var row = plan.Characters.ShouldHaveSingleItem();
        row.Action.ShouldBe(ProjectRestoreAction.Change);
        row.ApplyByDefault.ShouldBeFalse();
        row.Warnings.ShouldContain(ProjectRestoreWarning.ExportOlderThanBackup);
    }

    private ExistingClaimInfo DbClaim(int id, int characterId, ClaimStatus status, int? fee = null, string? wishesValue = null, DateTimeOffset? lastUpdated = null, int feePaid = 0, DateTimeOffset? changedByHandAt = null)
        => new(ClaimId(id), CharacterId(characterId), Player.UserId, status, Master.UserId, fee)
        {
            FieldLayer = Layer((wishes, wishesValue)),
            LastUpdated = lastUpdated ?? BackupAt.AddDays(-1),
            FeePaid = feePaid,
            LastChangedNotByRestore = changedByHandAt,
        };

    private static readonly DateTimeOffset MayExport = BackupAt.AddDays(10);

    [Fact]
    public void SuggestionEqualToCharacterValueIsNotProposed()
    {
        var plan = Plan(
            [
                CharacterExport(Day(5), CharacterRow(101, "Гамлет", [], strength: "10")),
                ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.AddedByUser, strength: "10")),
            ],
            characters: [DbCharacter(101, "Гамлет", strengthValue: "10")],
            claims: [DbClaim(3000, 101, ClaimStatus.AddedByUser)]);

        plan.ClaimFields.ShouldBeEmpty();
        plan.Claims.ShouldHaveSingleItem().Action.ShouldBe(ClaimRestoreAction.Nothing);
    }

    [Fact]
    public void SuggestionsAreDroppedWhenClaimIsApprovedAfterExport()
    {
        var plan = Plan(
            [ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.AddedByUser, strength: "100"))],
            characters: [DbCharacter(101, "Гамлет", strengthValue: "10")],
            claims: [DbClaim(3000, 101, ClaimStatus.AddedByUser)],
            notifications: [ClaimChange(Day(6), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(3000))]);

        plan.Claims.ShouldHaveSingleItem().Status.ShouldBe(ClaimStatus.Approved);
        plan.ClaimFields.ShouldBeEmpty();
    }

    [Fact]
    public void ApprovedClaimExportUpdatesCharacterFields()
    {
        var plan = Plan(
            [
                CharacterExport(Day(5), CharacterRow(101, "Гамлет", [], strength: "10")),
                ClaimExport(Day(6), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, strength: "12")),
            ],
            characters: [DbCharacter(101, "Гамлет", strengthValue: "5")],
            claims: [DbClaim(3000, 101, ClaimStatus.Approved)]);

        var field = plan.Characters.ShouldHaveSingleItem().Fields.ShouldHaveSingleItem();
        field.Value.ShouldBe("12");
        field.ExportedAt.ShouldBe(Day(6));
        field.ExportKind.ShouldBe(MasterExportKind.Claims);
        plan.ClaimFields.ShouldBeEmpty();
    }

    [Fact]
    public void FieldNotMappedInNewerExportComesFromOlder()
    {
        var older = ClaimExport(Day(3), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, wishes: "Корону"));
        var newer = new DatedMasterExport(
            new ParsedMasterExport(MasterExportKind.Claims, ProjectId.Value, null, ["Другое"],
                [ClaimRow(3000, "Гамлет", ClaimStatus.Approved) with { OtherCells = [new MasterExportCell("Другое", "x", false)] }], []),
            Day(5));

        var plan = Plan([older, newer]);

        var claim = plan.Claims.ShouldHaveSingleItem();
        claim.Action.ShouldBe(ClaimRestoreAction.Create);
        claim.Attributes.ShouldNotBeNull().Status.ShouldNotBeNull().Chosen.At.ShouldBe(Day(5));
        var field = plan.ClaimFields.ShouldHaveSingleItem().Fields.ShouldHaveSingleItem();
        field.Value.ShouldBe("Корону");
        field.ExportedAt.ShouldBe(Day(3));
    }

    [Fact]
    public void MultipleClaimExportsTakeLatest()
    {
        var plan = Plan([
            ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, wishes: "Новое")),
            ClaimExport(Day(3), ClaimRow(3000, "Гамлет", ClaimStatus.Discussed, wishes: "Старое")),
        ]);

        var claim = plan.Claims.ShouldHaveSingleItem();
        claim.Status.ShouldBe(ClaimStatus.Approved);
        plan.ClaimFields.ShouldHaveSingleItem().Fields.ShouldHaveSingleItem().Value.ShouldBe("Новое");
    }

    [Fact]
    public void ClaimChangedByHandAfterRestoreIsNotAppliedByDefault()
    {
        var plan = Plan(
            [ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved))],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.OnHold, lastUpdated: AfterRestore, changedByHandAt: AfterRestore)]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Change);
        row.ApplyByDefault.ShouldBeFalse();
        row.Warnings.ShouldContain(ClaimRestoreWarning.ChangedAfterRestore);
    }

    /// <summary>
    /// Заявку после аварии трогали только инструменты восстановления (#5619): создали по уведомлениям или
    /// восстановили комментарии майской заявки. Дата изменения поднята, но комментариев не от инструментов
    /// нет — это не ручная правка, хотя статус в базе расходится с уведомлениями.
    /// </summary>
    [Fact]
    public void ClaimTouchedOnlyByRestoreToolsIsAppliedByDefault()
    {
        var plan = Plan([],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.AddedByUser, lastUpdated: AfterRestore)],
            notifications: [ClaimChange(Day(2), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(3000))]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Change);
        row.Status.ShouldBe(ClaimStatus.Approved);
        row.ApplyByDefault.ShouldBeTrue();
        row.AttributesNotByDefault.ShouldBeEmpty();
        row.Warnings.ShouldNotContain(ClaimRestoreWarning.ChangedAfterRestore);
    }

    /// <summary>После аварии мастер сам сменил статус — в обсуждении есть его комментарий.</summary>
    [Fact]
    public void ClaimCommentedByHandAfterLossIsMarkedWithoutExports()
    {
        var plan = Plan([],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.OnHold, lastUpdated: AfterRestore, changedByHandAt: AfterRestore)],
            notifications: [ClaimChange(Day(2), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(3000))]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Status.ShouldBe(ClaimStatus.Approved);
        row.AttributesNotByDefault.ShouldBe([ClaimRestoreAttributeKind.Status]);
        row.ApplyByDefault.ShouldBeFalse();
        row.Warnings.ShouldContain(ClaimRestoreWarning.ChangedAfterRestore);
    }

    /// <summary>Комментарий не от инструментов, но до аварии, — не правка после восстановления.</summary>
    [Fact]
    public void ClaimCommentedBeforeLossIsNotMarked()
    {
        var plan = Plan([],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.OnHold, lastUpdated: AfterRestore, changedByHandAt: LostAt)],
            notifications: [ClaimChange(Day(2), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(3000))]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.ApplyByDefault.ShouldBeTrue();
        row.Warnings.ShouldNotContain(ClaimRestoreWarning.ChangedAfterRestore);
    }

    [Fact]
    public void ClaimAppliedFromOlderExportIsUpdatedByDefault()
    {
        var plan = Plan(
            [
                ClaimExport(MayExport, ClaimRow(3000, "Гамлет", ClaimStatus.Discussed)),
                ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved)),
            ],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.Discussed, lastUpdated: AfterRestore, changedByHandAt: AfterRestore)]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Change);
        row.ApplyByDefault.ShouldBeTrue();
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ClaimFieldsChangedByHandAfterRestoreAreNotAppliedByDefault()
    {
        var plan = Plan(
            [ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, wishes: "Корону"))],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.Approved, wishesValue: "Мастер дописал", lastUpdated: AfterRestore)]);

        var row = plan.ClaimFields.ShouldHaveSingleItem();
        row.ApplyByDefault.ShouldBeFalse();
        row.Warnings.ShouldContain(ProjectRestoreWarning.ChangedAfterRestore);
    }

    [Fact]
    public void SecondExportAfterAppliedFirstIsAppliedByDefault()
    {
        // Применён план по майской выгрузке: значения в базе — из неё.
        var mayApplied = DbCharacter(101, "Гамлет", groups: [11], strengthValue: "5", updatedAt: AfterRestore);
        var plan = Plan(
            [
                CharacterExport(MayExport, CharacterRow(101, "Гамлет", [(11, "Дания")], strength: "5")),
                CharacterExport(Day(5), CharacterRow(101, "Гамлет", [(11, "Дания"), (12, "Двор")], strength: "10")),
            ],
            groups: [new ExistingGroupInfo(GroupId(11), "Дания", IsActive: true)],
            characters: [mayApplied]);

        var row = plan.Characters.ShouldHaveSingleItem();
        row.Action.ShouldBe(ProjectRestoreAction.Change);
        row.Fields.ShouldHaveSingleItem().Value.ShouldBe("10");
        row.Groups.ShouldBe([GroupId(11), GroupId(12)], ignoreOrder: true);
        row.ApplyByDefault.ShouldBeTrue();
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void GroupWithUnknownNameIsNotSetByDefault()
    {
        var plan = Plan([CharacterExport(Day(5), CharacterRow(101, "Гамлет", [(11, "Дания"), (13, "")]))]);

        var group = plan.Groups.Single(g => g.GroupId == GroupId(13));
        group.Action.ShouldBe(ProjectRestoreAction.Create);
        group.Name.ShouldBeNull();
        group.ApplyByDefault.ShouldBeFalse();
        group.Warnings.ShouldContain(ProjectRestoreWarning.GroupNameUnknown);

        var hamlet = plan.Characters.ShouldHaveSingleItem();
        hamlet.Groups.ShouldBe([GroupId(11)]);
        hamlet.Warnings.ShouldContain(ProjectRestoreWarning.GroupSkipped);
    }

    [Fact]
    public void ClaimIsNotBoundToCharacterCreatedOnlyByDecision()
    {
        var plan = Plan(
            [
                CharacterExport(Day(2), CharacterRow(103, "Лаэрт", [])),
                CharacterExport(Day(5), CharacterRow(101, "Гамлет", [])),
            ],
            notifications: [ClaimChange(Day(6), CommentExtraAction.NewClaim, Player, [Master], characterName: "Лаэрт", claimId: ClaimId(2000))]);

        var laertes = plan.Characters.Single(c => c.CharacterId == CharacterId(103));
        laertes.Action.ShouldBe(ProjectRestoreAction.Create);
        laertes.ApplyByDefault.ShouldBeFalse();
        laertes.Warnings.ShouldContain(ProjectRestoreWarning.MissingInNewerExport);

        var claim = plan.Claims.ShouldHaveSingleItem();
        claim.Character.ShouldBeNull();
        claim.CharacterName.ShouldBe("Лаэрт");
    }

    [Fact]
    public void ClaimFieldsOfClaimNeedingDecisionAreNotAppliedByDefault()
    {
        var plan = Plan([ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, playerEmail: "stranger@example.com", wishes: "Корону"))]);

        plan.Claims.ShouldHaveSingleItem().NeedsManualChoice.ShouldBeTrue();
        var fields = plan.ClaimFields.ShouldHaveSingleItem();
        fields.ApplyByDefault.ShouldBeFalse();
        fields.Warnings.ShouldContain(ProjectRestoreWarning.ClaimNeedsDecision);
    }

    [Theory]
    [InlineData(1500, true)]
    [InlineData(2500, false)]
    public void TotalFeeBelowManualFeeIsMarked(int totalFee, bool expected)
    {
        var plan = Plan(
            [ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, totalFee: totalFee))],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.Approved, fee: 2000)]);

        plan.Claims.ShouldHaveSingleItem().Warnings.Contains(ClaimRestoreWarning.TotalFeeMismatch).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1000, 1000, false)]
    [InlineData(1000, 600, true)]
    [InlineData(0, 600, true)]
    [InlineData(null, 600, false)]
    public void FeePaidOfExistingClaimComparedWithDatabase(int? exportedPaid, int paidInDatabase, bool expected)
    {
        var plan = Plan(
            [ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, feePaid: exportedPaid))],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.Approved, feePaid: paidInDatabase)]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Warnings.Contains(ClaimRestoreWarning.FeePaidMismatch).ShouldBe(expected);
        row.FeePaidMismatch.ShouldBe(expected ? new FeePaidMismatch(exportedPaid!.Value, paidInDatabase) : null);
    }

    [Fact]
    public void FeePaidOfExistingClaimNotComparedWithExportOlderThanBackup()
    {
        // Оплаты между выгрузкой и бэкапом в базе есть, а в выгрузке — нет: расхождение было бы ложным.
        var plan = Plan(
            [ClaimExport(BackupAt.AddDays(-5), ClaimRow(3000, "Гамлет", ClaimStatus.AddedByUser, feePaid: 600))],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.AddedByUser, feePaid: 1000)],
            notifications: [ClaimChange(Day(4), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(3000))]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Status.ShouldBe(ClaimStatus.Approved);
        row.Warnings.ShouldNotContain(ClaimRestoreWarning.FeePaidMismatch);
        row.FeePaidMismatch.ShouldBeNull();
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(0, false)]
    [InlineData(null, false)]
    public void LostClaimWithFeePaidInExportIsMarked(int? exportedPaid, bool expected)
    {
        var plan = Plan(
            [ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, feePaid: exportedPaid))],
            characters: [DbCharacter(101, "Гамлет")]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Create);
        row.Warnings.Contains(ClaimRestoreWarning.FeePaidMismatch).ShouldBe(expected);
        row.FeePaidMismatch.ShouldBe(expected ? new FeePaidMismatch(exportedPaid!.Value, 0) : null);
    }

    [Fact]
    public void ClaimMovedBeforeExport()
    {
        var plan = Plan(
            [ClaimExport(Day(5), ClaimRow(3000, "Офелия", ClaimStatus.Approved))],
            characters: [DbCharacter(101, "Гамлет"), DbCharacter(102, "Офелия")],
            claims: [DbClaim(3000, 101, ClaimStatus.Approved)]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Action.ShouldBe(ClaimRestoreAction.Change);
        row.Character.ShouldBe(CharacterId(102));
    }

    [Fact]
    public void CharacterRenamedAfterExport()
    {
        var plan = Plan(
            [ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved))],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.Approved)],
            notifications: [ClaimChange(Day(6), action: null, Master, [Player], characterName: "Принц", claimId: ClaimId(3000))]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Character.ShouldBeNull();
        row.Warnings.ShouldContain(ClaimRestoreWarning.CharacterRenamed);
    }

    [Fact]
    public void LineEndingsDoNotMatter()
    {
        var plan = Plan(
            [CharacterExport(Day(5), CharacterRow(101, "Гамлет", [], strength: "Первая\nвторая"))],
            characters: [DbCharacter(101, "Гамлет", strengthValue: "Первая\r\nвторая")]);

        plan.Characters.ShouldHaveSingleItem().Action.ShouldBe(ProjectRestoreAction.Nothing);
    }

    [Fact]
    public void RowLinkedToOtherProjectIsSkipped()
    {
        var foreignRow = CharacterRow(102, "Офелия", []) with { Warnings = [MasterExportWarning.ProjectIdMismatch] };
        var plan = Plan([CharacterExport(Day(5), CharacterRow(101, "Гамлет", []), foreignRow)]);

        plan.Characters.ShouldHaveSingleItem().CharacterId.ShouldBe(CharacterId(101));
        plan.Notes.ShouldHaveSingleItem().Warning.ShouldBe(ProjectRestoreWarning.RowFromOtherProject);
    }

    private DatedMasterExport ClaimExportWithHeaders(DateTimeOffset at, string[] headers, params MasterExportRow[] rows)
        => new(new ParsedMasterExport(MasterExportKind.Claims, ProjectId.Value, null, headers, rows, []), at);

    [Fact]
    public void ClaimExportWithoutCharacterFieldsDoesNotBreak()
    {
        var plan = Plan(
            [ClaimExportWithHeaders(Day(5), ["Пожелания"],
                ClaimRow(3000, "Гамлет", ClaimStatus.Approved) with { OtherCells = [new MasterExportCell("Пожелания", "", false)] })],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.Approved)]);

        plan.Characters.ShouldBeEmpty();
        plan.Claims.ShouldHaveSingleItem().Action.ShouldBe(ClaimRestoreAction.Nothing);
    }

    [Fact]
    public void ExistingCharacterIsChangedAfterClaimApprovalCopiesSuggestions()
    {
        var notifications = new[]
        {
            ClaimChange(Day(6), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(3000)),
        };
        var exports = new[] { CharacterExport(Day(5), CharacterRow(101, "Гамлет", [], strength: "7")) };
        var claimLayer = Layer((strength, "5"));

        var plan = Plan(exports,
            characters: [DbCharacter(101, "Гамлет", strengthValue: "3")],
            claims: [DbClaim(3000, 101, ClaimStatus.AddedByUser) with { FieldLayer = claimLayer }],
            notifications: notifications);

        plan.CharacterCreations.ShouldBeEmpty();
        plan.Claims.ShouldHaveSingleItem().Status.ShouldBe(ClaimStatus.Approved);
        var change = plan.CharacterChanges.ShouldHaveSingleItem();
        plan.ClaimFields.ShouldBeEmpty();

        // Применяем в порядке плана: одобрение переносит слой заявки в персонажа, затем — изменения персонажа.
        var characterValues = new Dictionary<ProjectFieldIdentification, string?> { [strength.Id] = "3" };
        foreach (var (field, value) in claimLayer.LayerData)
        {
            characterValues[field] = value.Value;
        }
        foreach (var field in change.Fields.Where(f => f.ApplyByDefault))
        {
            characterValues[field.Field] = field.Value;
        }
        characterValues[strength.Id].ShouldBe("7");

        var replan = Plan(exports,
            characters: [DbCharacter(101, "Гамлет", strengthValue: "7", updatedAt: AfterRestore)],
            claims: [DbClaim(3000, 101, ClaimStatus.Approved, lastUpdated: AfterRestore)],
            notifications: notifications);

        replan.CharacterChanges.ShouldHaveSingleItem().Action.ShouldBe(ProjectRestoreAction.Nothing);
        replan.Claims.ShouldHaveSingleItem().Action.ShouldBe(ClaimRestoreAction.Nothing);
        replan.ClaimFields.ShouldBeEmpty();
    }

    [Fact]
    public void ChangedByHandIsMarkedPerField()
    {
        var row = CharacterRow(101, "Гамлет", [], strength: "10") with
        {
            OtherCells = [new MasterExportCell("Сила", "10", false), new MasterExportCell("Ловкость", "3", false)],
        };
        var export = new DatedMasterExport(
            new ParsedMasterExport(MasterExportKind.Characters, ProjectId.Value, null, ["Сила", "Ловкость"], [row], []),
            Day(5));

        var plan = Plan([export], characters: [DbCharacter(101, "Гамлет", strengthValue: "7", updatedAt: AfterRestore)]);

        var character = plan.Characters.ShouldHaveSingleItem();
        character.ApplyByDefault.ShouldBeTrue();
        var strengthChange = character.Fields.Single(f => f.Field == strength.Id);
        strengthChange.ApplyByDefault.ShouldBeFalse();
        strengthChange.RestoreWarnings.ShouldBe([ProjectRestoreWarning.ChangedAfterRestore]);
        // Пустое значение правкой не считается.
        var agilityChange = character.Fields.Single(f => f.Field == agility.Id);
        agilityChange.ApplyByDefault.ShouldBeTrue();
        agilityChange.RestoreWarnings.ShouldBeEmpty();
    }

    [Fact]
    public void ClaimAttributeChangedByHandIsMarkedSeparately()
    {
        var plan = Plan(
            [
                ClaimExport(MayExport, ClaimRow(3000, "Гамлет", ClaimStatus.Discussed)),
                ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved)),
            ],
            characters: [DbCharacter(101, "Гамлет")],
            claims: [DbClaim(3000, 101, ClaimStatus.Discussed, lastUpdated: AfterRestore, changedByHandAt: AfterRestore) with { ResponsibleMaster = OtherMaster.UserId }]);

        var row = plan.Claims.ShouldHaveSingleItem();
        row.Status.ShouldBe(ClaimStatus.Approved);
        row.ResponsibleMaster.ShouldBe(Master.UserId);
        row.AttributesNotByDefault.ShouldBe([ClaimRestoreAttributeKind.ResponsibleMaster]);
        row.ApplyByDefault.ShouldBeTrue();
        row.Warnings.ShouldContain(ClaimRestoreWarning.ChangedAfterRestore);
    }

    [Fact]
    public void SuggestionEqualToCharacterClearsCurrentSuggestion()
    {
        var exports = new[] { ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.AddedByUser, strength: "10")) };
        var characters = new[] { DbCharacter(101, "Гамлет", strengthValue: "10") };

        var plan = Plan(exports, characters: characters,
            claims: [DbClaim(3000, 101, ClaimStatus.AddedByUser) with { FieldLayer = Layer((strength, "5")) }]);

        var field = plan.ClaimFields.ShouldHaveSingleItem().Fields.ShouldHaveSingleItem();
        field.Value.ShouldBe("10");
        field.CurrentValue.ShouldBe("5");

        // Домен выбросил значение, равное персонажу, — слой пуст, выгрузка из базы показала бы то же.
        Plan(exports, characters: characters, claims: [DbClaim(3000, 101, ClaimStatus.AddedByUser)])
            .ClaimFields.ShouldBeEmpty();
    }

    [Fact]
    public void ApprovedLostClaimCreatingCharacterKeepsCharacterFields()
    {
        var plan = Plan([ClaimExport(Day(5), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, strength: "12"))]);

        var claim = plan.Claims.ShouldHaveSingleItem();
        claim.Action.ShouldBe(ClaimRestoreAction.Create);
        claim.Character.ShouldBeNull();
        var field = plan.ClaimFields.ShouldHaveSingleItem().Fields.ShouldHaveSingleItem();
        field.Field.ShouldBe(strength.Id);
        field.Value.ShouldBe("12");
    }

    [Fact]
    public void SuggestionsComeOnlyFromExportsAfterLastApproval()
    {
        var plan = Plan(
            [
                ClaimExport(Day(3), ClaimRow(3000, "Гамлет", ClaimStatus.AddedByUser, strength: "8")),
                ClaimExport(Day(4), ClaimRow(3000, "Гамлет", ClaimStatus.Approved, strength: "10")),
            ],
            characters: [DbCharacter(101, "Гамлет", strengthValue: "10")],
            claims: [DbClaim(3000, 101, ClaimStatus.Approved)],
            notifications: [ClaimChange(Day(6), CommentExtraAction.DeclineByMaster, Master, [Player], characterName: "Гамлет", claimId: ClaimId(3000))]);

        plan.Claims.ShouldHaveSingleItem().Status.ShouldBe(ClaimStatus.DeclinedByMaster);
        plan.ClaimFields.ShouldBeEmpty();
    }

    [Fact]
    public void GroupsAppliedWithoutUnusableGroupAreNotManualEdit()
    {
        var plan = Plan(
            [
                CharacterExport(MayExport, CharacterRow(101, "Гамлет", [(11, "Дания"), (12, "Чужая")])),
                CharacterExport(Day(5), CharacterRow(101, "Гамлет", [(11, "Дания"), (13, "Двор")])),
            ],
            groups: [new ExistingGroupInfo(GroupId(11), "Дания", IsActive: true)],
            characters: [DbCharacter(101, "Гамлет", groups: [11], updatedAt: AfterRestore)],
            groupIdsTakenElsewhere: [12]);

        var row = plan.Characters.ShouldHaveSingleItem();
        row.Groups.ShouldBe([GroupId(11), GroupId(13)], ignoreOrder: true);
        row.GroupsApplyByDefault.ShouldBeTrue();
        row.Warnings.ShouldNotContain(ProjectRestoreWarning.ChangedAfterRestore);
    }
}
