using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Drp;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Drp.MasterExports;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using JoinRpg.WebPortal.Managers.Drp;

namespace JoinRpg.WebPortal.Managers.Test.Drp;

public class ProjectRestorePlanViewBuilderTest
{
    private static readonly DateTimeOffset At = new(2026, 6, 1, 10, 0, 0, TimeSpan.FromHours(3));

    private readonly MockedProject mock = new();

    private static string DisplayName<T>(T value) where T : struct, Enum
        => typeof(T).GetField(value.ToString())!.GetCustomAttribute<DisplayAttribute>()?.Name ?? "";

    [Fact]
    public void EveryProjectWarningHasRussianText()
    {
        foreach (var warning in Enum.GetValues<ProjectRestoreWarning>())
        {
            DisplayName(ProjectRestorePlanViewBuilder.ToWarningView(warning)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void EveryFieldWarningHasRussianText()
    {
        foreach (var warning in Enum.GetValues<MasterExportFieldWarning>())
        {
            DisplayName(ProjectRestorePlanViewBuilder.ToWarningView(warning)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void EveryExportWarningHasRussianText()
    {
        foreach (var warning in Enum.GetValues<MasterExportWarning>())
        {
            DisplayName(ProjectRestorePlanViewBuilder.ToWarningView(warning)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void EveryFormatErrorHasRussianText()
    {
        foreach (var error in Enum.GetValues<MasterExportFormatError>())
        {
            DisplayName(ProjectRestorePlanViewBuilder.ToErrorView(error)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void EveryKindAndSourceIsMapped()
    {
        foreach (var kind in Enum.GetValues<MasterExportKind>())
        {
            DisplayName(ProjectRestorePlanViewBuilder.ToKindView(kind)).ShouldNotBeNullOrWhiteSpace();
        }
        foreach (var source in Enum.GetValues<RestoreSourceKind>())
        {
            DisplayName(ClaimRestorePlanBuilder.ToSourceView(source)).ShouldNotBeNullOrWhiteSpace();
        }
        foreach (var attribute in Enum.GetValues<ClaimRestoreAttributeKind>())
        {
            ClaimRestorePlanBuilder.ToAttributeKindView(attribute).ToString().ShouldBe(attribute.ToString());
        }
    }

    [Fact]
    public void EveryClaimWarningHasRussianText()
    {
        foreach (var warning in Enum.GetValues<ClaimRestoreWarning>())
        {
            DisplayName(ClaimRestorePlanBuilder.ToWarningView(warning)).ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Payload_SurvivesRoundTrip()
    {
        var export = new ParsedMasterExport(
            MasterExportKind.Claims,
            ProjectId: 42,
            SuggestedDate: At,
            OtherHeaders: ["Легенда", "Возраст"],
            Rows: [new MasterExportRow(
                RowNumber: 3,
                CharacterId: null,
                ClaimId: 777,
                Name: "Герой",
                Groups: [new MasterExportGroupRef(5, "Отряд")],
                ResponsibleMasterName: "Мастер",
                PlayerEmail: "player@example.com",
                PlayerDisplayName: "Игрок",
                Status: ClaimStatus.Approved,
                TotalFee: 1500,
                FeePaid: 1000,
                ClaimCreatedAt: At.AddDays(-3),
                ClaimUpdatedAt: At.AddDays(-1),
                OtherCells: [new MasterExportCell("Легенда", "Длинный текст", Truncated: true), new MasterExportCell("Возраст", "", false)])
            {
                Warnings = [MasterExportWarning.TruncatedValues],
                CharacterType = new MasterExportCharacterType(CharacterType.Slot, IsHot: true),
            }],
            Warnings: [MasterExportWarning.DuplicateHeaders, MasterExportWarning.TruncatedValues]);

        var restored = MasterExportPayload.Deserialize(MasterExportPayload.Serialize(export));

        JsonSerializer.Serialize(restored).ShouldBe(JsonSerializer.Serialize(export));
        var row = restored.Rows.ShouldHaveSingleItem();
        row.Warnings.ShouldBe([MasterExportWarning.TruncatedValues]);
        row.FeePaid.ShouldBe(1000);
        row.ClaimCreatedAt.ShouldBe(At.AddDays(-3));
        row.ClaimCreatedAt!.Value.Offset.ShouldBe(TimeSpan.FromHours(3));
        row.OtherCells[0].Truncated.ShouldBeTrue();
        row.CharacterType.ShouldBe(new MasterExportCharacterType(CharacterType.Slot, IsHot: true));
        restored.SuggestedDate.ShouldBe(At);
    }

    [Theory]
    [InlineData("""{"Type":1,"IsHot":true}""")]
    [InlineData("""{"Type":42,"IsHot":false}""")]
    public void Payload_InvalidCharacterType_FormatException(string characterType)
        => Should.Throw<FormatException>(() => MasterExportPayload.Deserialize(Gzip(
            $$"""{"Kind":0,"ProjectId":1,"OtherHeaders":[],"Warnings":[],"Rows":[{"RowNumber":2,"Groups":[],"OtherCells":[],"Warnings":[],"CharacterType":{{characterType}}}]}""")));

    [Fact]
    public void Payload_WithoutCharacterType_TypeUnknown()
        => MasterExportPayload.Deserialize(Gzip(
                """{"Kind":0,"ProjectId":1,"OtherHeaders":[],"Warnings":[],"Rows":[{"RowNumber":2,"Groups":[],"OtherCells":[],"Warnings":[]}]}"""))
            .Rows.ShouldHaveSingleItem().CharacterType.ShouldBeNull();

    [Fact]
    public void Payload_Garbage_FormatException()
    {
        _ = Should.Throw<FormatException>(() => MasterExportPayload.Deserialize("не base64"));
        _ = Should.Throw<FormatException>(() => MasterExportPayload.Deserialize(Convert.ToBase64String("{}"u8.ToArray())));
        _ = Should.Throw<FormatException>(() => MasterExportPayload.Deserialize(null));
        _ = Should.Throw<FormatException>(() => MasterExportPayload.Deserialize(Gzip("null")));
        _ = Should.Throw<FormatException>(() => MasterExportPayload.Deserialize(Gzip("{}")));
        _ = Should.Throw<FormatException>(() => MasterExportPayload.Deserialize(Gzip("""{"Kind":0,"ProjectId":1,"OtherHeaders":[],"Rows":[null],"Warnings":[]}""")));
    }

    [Fact]
    public void Payload_Bomb_FormatException()
    {
        // 2 МБ пробелов сжимаются в килобайты, а распаковываются за предел.
        var bomb = Gzip(new string(' ', 2 * 1024 * 1024) + "{}");

        _ = Should.Throw<FormatException>(() => MasterExportPayload.Deserialize(bomb, maxDecompressedSize: 1024 * 1024));
    }

    private static string Gzip(string json)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(buffer, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(System.Text.Encoding.UTF8.GetBytes(json));
        }
        return Convert.ToBase64String(buffer.ToArray());
    }

    [Fact]
    public void BuildSource_TakesCharacterLayerRegularGroupsAndDates()
    {
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(mock.CharacterFieldInfo, "значение"));
        mock.Character.UpdatedAt = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Unspecified);
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.LastUpdateDateTime = new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Unspecified);
        MockedProject.AssignFieldValues(claim, new FieldWithValue(mock.CharacterFieldInfo, "предложение игрока"));

        var changedByHandAt = new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
        var source = ClaimRestorePlanBuilder.BuildSource(
            mock.ProjectInfo, [], [.. mock.Project.Characters.Select(mock.GetCharacterInfo)], At,
            new Dictionary<ClaimIdentification, DateTimeOffset> { [claim.GetId()] = changedByHandAt });

        var character = source.Characters.Single(c => c.CharacterId.CharacterId == mock.Character.CharacterId);
        var characterLayer = character.FieldLayer.ShouldNotBeNull();
        characterLayer.ProjectInfo.ShouldBeSameAs(mock.ProjectInfo);
        characterLayer.GetValue(mock.CharacterFieldInfo).ShouldBe("значение");
        characterLayer.LayerData.Values.ShouldAllBe(f => f.Field.BoundTo == FieldBoundTo.Character);
        character.UpdatedAt.ShouldBe(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
        character.Groups.ShouldAllBe(g => mock.ProjectInfo.GroupTree.GetGroupById(g).GroupType == CharacterGroupType.Regular);
        character.Groups.ShouldNotContain(mock.ProjectInfo.GroupTree.RootGroupId);
        character.CharacterType.ShouldBe(CharacterType.Player);

        var existing = source.Claims.ShouldHaveSingleItem();
        existing.LastUpdated.ShouldBe(new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero));
        existing.LastChangedNotByRestore.ShouldBe(changedByHandAt);
        // Слой заявки — её собственный, значения персонажа в него не подмешиваются.
        existing.FieldLayer.ShouldNotBeNull().GetValue(mock.CharacterFieldInfo).ShouldBe("предложение игрока");
    }

    [Fact]
    public void Build_FieldChangeShowsCurrentAndNewValue()
    {
        var source = new ProjectRestoreSource(
            mock.ProjectInfo,
            ClaimRestorePlanBuilder.BuildSource(mock.ProjectInfo, [], [.. mock.Project.Characters.Select(mock.GetCharacterInfo)], At),
            [], [], new Dictionary<string, UserIdentification>(), At);
        var characterId = new CharacterIdentification(mock.ProjectInfo.ProjectId, mock.Character.CharacterId);
        var change = new FieldRestoreChange(mock.CharacterFieldInfo.Id, "новое", "старое", At, MasterExportKind.Characters,
            ApplyByDefault: false, [MasterExportFieldWarning.Truncated], [ProjectRestoreWarning.ChangedAfterRestore]);
        var plan = new ProjectRestorePlan(
            [new MasterExportPlanInfo(MasterExportKind.Characters, At, true, [
                new MasterExportColumnMapping("Легенда", mock.CharacterFieldInfo.Id, []),
                new MasterExportColumnMapping("Комментарий", null, [MasterExportFieldWarning.ColumnNotMapped])])],
            [],
            [],
            [],
            [new CharacterRestorePlanRow(characterId, ProjectRestoreAction.Change, null, null, [], [change], null, At, false, [ProjectRestoreWarning.ChangedAfterRestore])],
            [],
            [new ProjectRestoreNote(ProjectRestoreWarning.RowWithoutId, At, 5)]);

        var model = ProjectRestorePlanViewBuilder.Build(mock.ProjectInfo, source, plan, [], new DrpOptions { BackupTakenAt = At, LostAt = At }, canEditRoles: false);

        model.CanEditRoles.ShouldBeFalse();
        var field = model.CharacterChanges.ShouldHaveSingleItem().Fields.ShouldHaveSingleItem();
        field.FieldName.ShouldBe(mock.CharacterFieldInfo.Name);
        field.CurrentDisplay.ShouldBe("старое");
        field.NewDisplay.ShouldBe("новое");
        field.Value.ShouldBe("новое");
        field.ApplyByDefault.ShouldBeFalse();
        field.Warnings.ShouldBe([MasterExportFieldWarningView.Truncated]);
        field.RestoreWarnings.ShouldBe([ProjectRestoreWarningView.ChangedAfterRestore]);
        var export = model.Exports.ShouldHaveSingleItem();
        export.MappedCount.ShouldBe(1);
        export.Columns.ShouldHaveSingleItem().Header.ShouldBe("Комментарий");
        model.Notes.ShouldHaveSingleItem().RowNumber.ShouldBe(5);
    }

    /// <summary>План с уведомлениями о принятии заявок: одна с расхождением имён и <paramref name="matching"/> без.</summary>
    private ClaimRestorePlanViewModel BuildWithNameEvents(int matching = 1)
    {
        ClaimNotificationEvent Event(int claimId, string characterName)
            => new(new ClaimIdentification(mock.ProjectInfo.ProjectId, claimId), new ClaimIdentification(mock.ProjectInfo.ProjectId, claimId),
                At, new UserIdentification(1), ClaimNotificationKind.ClaimChange,
                characterName, "Игрок", CommentExtraAction.ApproveByMaster, ClaimEventAuthor.Master, null, null, "", []);
        var source = new ProjectRestoreSource(
            mock.ProjectInfo,
            ClaimRestorePlanBuilder.BuildSource(mock.ProjectInfo,
                [.. Enumerable.Range(9100, matching).Select(id => Event(id, "Игрок")), Event(9002, "Персонаж")],
                [.. mock.Project.Characters.Select(mock.GetCharacterInfo)], At),
            [], [], new Dictionary<string, UserIdentification>(), At);
        var plan = new ProjectRestorePlan([], [], [], [], [], [], []);
        return ProjectRestorePlanViewBuilder.Build(mock.ProjectInfo, source, plan, [], new DrpOptions { BackupTakenAt = At, LostAt = At }, canEditRoles: true);
    }

    /// <summary>Имена персонажей расходились с именами игроков, а поле-имя не выбрано — предупреждаем.</summary>
    [Fact]
    public void Build_NameMismatchWithoutNameField_Warns()
        => BuildWithNameEvents().CharacterNameMismatch.ShouldBe(new CharacterNameMismatchView(2, 1));

    /// <summary>Расхождений не больше 10% — предупреждать не о чем.</summary>
    [Fact]
    public void Build_FewNameMismatches_NoWarning()
        => BuildWithNameEvents(matching: 9).CharacterNameMismatch.ShouldBeNull();

    /// <summary>С выбранным полем-именем персонажи получат имена из уведомлений — предупреждать не о чем.</summary>
    [Fact]
    public void Build_NameMismatchWithNameField_NoWarning()
    {
        var nameField = mock.AddField(field =>
        {
            field.FieldName = "Имя персонажа";
            field.FieldType = ProjectFieldType.String;
            field.FieldBoundTo = FieldBoundTo.Character;
        });
        mock.Project.Details.CharacterNameField = mock.Project.ProjectFields.Single(f => f.ProjectFieldId == nameField.Id.ProjectFieldId);
        mock.ReInitProjectInfo();

        BuildWithNameEvents().CharacterNameMismatch.ShouldBeNull();
    }

    /// <summary>Поле-имя не выбрано, а похожее поле есть — восстановленные персонажи получат имена игроков.</summary>
    [Fact]
    public void Build_WarnsAboutUnboundNameField()
    {
        _ = mock.CreateField("Имя персонажа");
        var source = new ProjectRestoreSource(
            mock.ProjectInfo,
            ClaimRestorePlanBuilder.BuildSource(mock.ProjectInfo, [], [.. mock.Project.Characters.Select(mock.GetCharacterInfo)], At),
            [], [], new Dictionary<string, UserIdentification>(), At);
        var plan = new ProjectRestorePlan([], [], [], [], [], [], []);

        var model = ProjectRestorePlanViewBuilder.Build(mock.ProjectInfo, source, plan, [], new DrpOptions { BackupTakenAt = At, LostAt = At }, canEditRoles: true);

        model.UnboundNameFieldCandidates.ShouldBe(["Имя персонажа"]);
    }

    /// <summary>Тип из «Занят?» доходит до строки создания; неизвестный — персонаж игрока, как раньше (#5645).</summary>
    [Fact]
    public void Build_CreationCarriesCharacterType()
    {
        var source = new ProjectRestoreSource(
            mock.ProjectInfo,
            ClaimRestorePlanBuilder.BuildSource(mock.ProjectInfo, [], [.. mock.Project.Characters.Select(mock.GetCharacterInfo)], At),
            [], [], new Dictionary<string, UserIdentification>(), At);
        CharacterRestorePlanRow Creation(int id, MasterExportCharacterType? type)
            => new(new CharacterIdentification(mock.ProjectInfo.ProjectId, id), ProjectRestoreAction.Create, "П", [], [], [], null, At, true, [])
            {
                ExportCharacterType = type,
            };
        var plan = new ProjectRestorePlan(
            [], [],
            [Creation(9001, new MasterExportCharacterType(CharacterType.Slot, IsHot: true)), Creation(9002, null)],
            [], [], [], []);

        var model = ProjectRestorePlanViewBuilder.Build(mock.ProjectInfo, source, plan, [], new DrpOptions { BackupTakenAt = At, LostAt = At }, canEditRoles: true);

        model.CharacterCreations.Select(c => (c.CharacterType, c.IsHot)).ShouldBe([(CharacterType.Slot, true), (CharacterType.Player, false)]);
    }

    [Fact]
    public void Build_ClaimFieldsExistInDatabaseFlag()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var claimId = claim.GetId();
        var missingClaimId = new ClaimIdentification(mock.ProjectInfo.ProjectId, claimId.ClaimId + 1000);
        var source = new ProjectRestoreSource(
            mock.ProjectInfo,
            ClaimRestorePlanBuilder.BuildSource(mock.ProjectInfo, [], [.. mock.Project.Characters.Select(mock.GetCharacterInfo)], At),
            [], [], new Dictionary<string, UserIdentification>(), At);
        var change = new FieldRestoreChange(mock.CharacterFieldInfo.Id, "новое", "старое", At, MasterExportKind.Claims,
            ApplyByDefault: true, [], []);
        var plan = new ProjectRestorePlan(
            [],
            [],
            [],
            [],
            [],
            [
                new ClaimFieldsRestorePlanRow(claimId, [change], At, true, []),
                new ClaimFieldsRestorePlanRow(missingClaimId, [change], At, true, []),
            ],
            []);

        var model = ProjectRestorePlanViewBuilder.Build(mock.ProjectInfo, source, plan, [], new DrpOptions { BackupTakenAt = At, LostAt = At }, canEditRoles: false);

        var existing = model.ClaimFields.Single(c => c.ClaimId == claimId);
        existing.ExistsInDatabase.ShouldBeTrue();
        var missing = model.ClaimFields.Single(c => c.ClaimId == missingClaimId);
        missing.ExistsInDatabase.ShouldBeFalse();
    }

    /// <summary>Модели плана по выгрузкам ездят в остров по JSON.</summary>
    [Fact]
    public void ViewModels_SurviveJson()
    {
        var projectId = new ProjectIdentification(1);
        var field = new FieldRestoreChangeView(new ProjectFieldIdentification(projectId, 3), "Поле", "1,2", "", "А, Б", At,
            MasterExportKindView.Claims, true, [MasterExportFieldWarningView.Truncated], []);
        var plan = new ClaimRestorePlanViewModel(projectId, null, At, At, [], [])
        {
            CanEditRoles = true,
            Groups = [new GroupRestoreRowViewModel(new CharacterGroupIdentification(projectId, 5), ClaimRestoreActionView.Create, "Г", null, At, true, [])],
            CharacterCreations = [new CharacterRestoreRowViewModel(new CharacterIdentification(projectId, 7), ClaimRestoreActionView.Create, "П", null,
                [new RestoreGroupOption(new CharacterGroupIdentification(projectId, 5), "Г")], [], true, [field], At, true, [ProjectRestoreWarningView.GroupSkipped])
            {
                CharacterType = CharacterType.Slot,
                IsHot = true,
            }],
            ClaimFields = [new ClaimFieldsRestoreRowViewModel(new ClaimIdentification(projectId, 9), "П", [field], At, true, [], ExistsInDatabase: true)],
            Exports = [new MasterExportPlanInfoView(MasterExportKindView.Characters, At, true, 2, [new MasterExportColumnView("X", null, [MasterExportFieldWarningView.ColumnNotMapped])])],
            Notes = [new ProjectRestoreNoteView(ProjectRestoreWarningView.DuplicateRow, At, 4)],
        };

        var restored = JsonRoundTrip.Ensure(plan);

        restored.CanEditRoles.ShouldBeTrue();
        restored.Groups.ShouldHaveSingleItem().GroupId.ShouldBe(plan.Groups[0].GroupId);
        var creation = restored.CharacterCreations.ShouldHaveSingleItem();
        creation.Fields.ShouldHaveSingleItem().FieldId.ShouldBe(field.FieldId);
        creation.CharacterType.ShouldBe(CharacterType.Slot);
        creation.IsHot.ShouldBeTrue();
        restored.ClaimFields.ShouldHaveSingleItem().ClaimId.ShouldBe(plan.ClaimFields[0].ClaimId);
        restored.Notes.ShouldHaveSingleItem().ShouldBe(plan.Notes[0]);

        var request = new CharacterRestoreApplyRequest(new CharacterIdentification(projectId, 7), true, "П",
            [new CharacterGroupIdentification(projectId, 5)], [new RestoreFieldValueRequest(field.FieldId, null)])
        {
            CharacterType = CharacterType.NonPlayer,
        };
        var restoredRequest = JsonRoundTrip.Ensure(request);
        restoredRequest.CharacterType.ShouldBe(CharacterType.NonPlayer);
        restoredRequest.Groups.ShouldNotBeNull().ShouldBe(request.Groups!);
        restoredRequest.Fields.ShouldBe(request.Fields);

        var upload = new MasterExportUploadResult(new MasterExportUploadViewModel("a.xlsx", MasterExportKindView.Characters, 1, At, 3, [], "p"), null);
        var restoredUpload = JsonRoundTrip.Ensure(upload).Export.ShouldNotBeNull();
        restoredUpload.Payload.ShouldBe("p");
        restoredUpload.SuggestedDate.ShouldBe(At);
    }
}
