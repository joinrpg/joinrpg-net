using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Web.Claims;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.ClaimRestore;

public class ProjectRestoreRowEditorsTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly DateTimeOffset At = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly CharacterGroupIdentification Group = new(ProjectId, 50);
    private static readonly CharacterIdentification Character = new(ProjectId, 900);
    private static readonly ProjectFieldIdentification FieldA = new(ProjectId, 1);
    private static readonly ProjectFieldIdentification FieldB = new(ProjectId, 2);

    private static FieldRestoreChangeView Field(ProjectFieldIdentification id, bool byDefault)
        => new(id, "Поле", "значение", "", "значение", At, MasterExportKindView.Characters, byDefault, [], []);

    private static CharacterRestoreRowViewModel CharacterRow(ClaimRestoreActionView action, bool groupsByDefault = true, string? name = "Герой")
        => new(Character, action, name, null, [new RestoreGroupOption(Group, "Г")], [], groupsByDefault,
            [Field(FieldA, true), Field(FieldB, false)], At, true, []);

    [Fact]
    public void Character_OnlyFieldsByDefaultAreSent()
    {
        var editor = new CharacterRestoreRowEditor(CharacterRow(ClaimRestoreActionView.Change), canEditRoles: true);

        editor.Selected.ShouldBeTrue();
        var request = editor.ToApplyRequest();
        request.Create.ShouldBeFalse();
        request.Name.ShouldBeNull();
        request.Groups.ShouldBe([Group]);
        request.Fields.ShouldBe([new RestoreFieldValueRequest(FieldA, "значение")]);
    }

    /// <summary>Тип нового персонажа из плана (по «Занят?» выгрузки) уходит в запрос создания (#5645).</summary>
    [Fact]
    public void Character_CreateSendsCharacterType()
    {
        var row = CharacterRow(ClaimRestoreActionView.Create) with { CharacterType = CharacterType.Slot, IsHot = true };

        var request = new CharacterRestoreRowEditor(row, canEditRoles: true).ToApplyRequest();

        request.Create.ShouldBeTrue();
        request.CharacterType.ShouldBe(CharacterType.Slot);
        request.IsHot.ShouldBeTrue();
    }

    [Fact]
    public void Character_GroupsNotByDefault_NotSent()
    {
        var editor = new CharacterRestoreRowEditor(CharacterRow(ClaimRestoreActionView.Change, groupsByDefault: false), canEditRoles: true);

        editor.ToApplyRequest().Groups.ShouldBeNull();
    }

    [Fact]
    public void Character_NothingChecked_Blocked()
    {
        var editor = new CharacterRestoreRowEditor(CharacterRow(ClaimRestoreActionView.Change), canEditRoles: true);
        editor.ApplyGroups = false;
        editor.Fields[0].Selected = false;

        editor.BlockingProblem.ShouldNotBeNull();
    }

    [Fact]
    public void Character_CreateWithoutName_BlockedAndNotSelected()
    {
        var editor = new CharacterRestoreRowEditor(CharacterRow(ClaimRestoreActionView.Create, name: null), canEditRoles: true);

        editor.Selected.ShouldBeFalse();
        editor.BlockingProblem.ShouldBe("Укажите имя персонажа");
        editor.Name = " Герой ";
        editor.ToApplyRequest().Name.ShouldBe("Герой");
        editor.ToApplyRequest().Create.ShouldBeTrue();
    }

    [Fact]
    public void Character_WithoutEditRoles_CannotApply()
    {
        var editor = new CharacterRestoreRowEditor(CharacterRow(ClaimRestoreActionView.Create), canEditRoles: false);

        editor.CanApply.ShouldBeFalse();
        editor.Selected.ShouldBeFalse();
    }

    [Fact]
    public void Group_UnknownName_NotSelected()
    {
        var editor = new GroupRestoreRowEditor(
            new GroupRestoreRowViewModel(Group, ClaimRestoreActionView.Create, null, null, At, false, [ProjectRestoreWarningView.GroupNameUnknown]),
            canEditRoles: true);

        editor.Selected.ShouldBeFalse();
        editor.BlockingProblem.ShouldNotBeNull();
        editor.Name = "Отряд";
        editor.ToApplyRequest().ShouldBe(new GroupRestoreApplyRequest(Group, "Отряд"));
    }

    [Fact]
    public void ClaimFields_KeepsSelectionsAfterReload()
    {
        var row = new ClaimFieldsRestoreRowViewModel(new ClaimIdentification(ProjectId, 3), "Герой", [Field(FieldA, true), Field(FieldB, false)], At, true, [], ExistsInDatabase: true);
        var previous = new ClaimFieldsRestoreRowEditor(row);
        previous.Fields[1].Selected = true;

        var reloaded = new ClaimFieldsRestoreRowEditor(row);
        reloaded.CopyEditsFrom(previous);

        reloaded.ToApplyRequest().Fields.Count.ShouldBe(2);
    }

    [Fact]
    public void ClaimEditor_AttributeChangedByHand_NotChangedByDefault()
    {
        var player = new UserLinkViewModel(new UserIdentification(10), "Игрок", ViewMode.Show);
        var master = new UserIdentification(2);
        var row = new ClaimRestoreRowViewModel(
            new ClaimIdentification(ProjectId, 3), ClaimRestoreActionView.Change, "Герой", null, null, [], player, [player],
            ClaimStatusView.AddedByUser, ClaimStatusView.Approved, At, master, master, 100, 200, At, At,
            [ClaimRestoreWarningView.ChangedAfterRestore])
        {
            AttributesNotByDefault = [ClaimRestoreAttributeKindView.ManualFee],
            Attributes = new ClaimRestoreAttributesView(
                new RestoreAttributeView<ClaimStatusView>(
                    new RestoreSourcedValueView<ClaimStatusView>(ClaimStatusView.Approved, RestoreSourceKindView.Notification, At),
                    [new RestoreSourcedValueView<ClaimStatusView>(ClaimStatusView.OnHold, RestoreSourceKindView.Export, At.AddDays(-1))]),
                null, null),
        };

        var editor = new ClaimRestoreRowEditor(row);

        // Пометка «правили руками» решается по атрибутам — строка остаётся отмеченной.
        editor.Selected.ShouldBeTrue();
        editor.ApplyFee.ShouldBeFalse();
        editor.ToApplyRequest(new Dictionary<string, CharacterIdentification>()).ManualFee.ShouldBeNull();

        // Мастер выбрал статус из выгрузки — время берётся у него.
        editor.Status = ClaimStatusView.OnHold;
        var request = editor.ToApplyRequest(new Dictionary<string, CharacterIdentification>());
        request.Status.ShouldBe(ClaimStatusView.OnHold);
        request.StatusChangedAt.ShouldBe(At.AddDays(-1));
    }

    [Fact]
    public void Dependencies_PendingUntilCreated()
    {
        var claimId = new ClaimIdentification(ProjectId, 3);
        var plan = new ClaimRestorePlanViewModel(ProjectId, null, At, At,
            [new ClaimRestoreRowViewModel(claimId, ClaimRestoreActionView.Create, "Герой", null, null, [], null, [], null, null, null, null, null, null, null, At, At, [])],
            [])
        {
            Groups = [new GroupRestoreRowViewModel(Group, ClaimRestoreActionView.Create, "Г", null, At, true, [])],
            CharacterCreations = [CharacterRow(ClaimRestoreActionView.Create)],
        };
        var dependencies = new RestoreDependencies(plan);

        dependencies.GroupsProblem([Group]).ShouldNotBeNull();
        dependencies.CharacterProblem(Character).ShouldNotBeNull();
        dependencies.ClaimProblem(claimId).ShouldNotBeNull();
        dependencies.CharacterProblem(new CharacterIdentification(ProjectId, 1)).ShouldBeNull();
        dependencies.GroupsProblem(null).ShouldBeNull();

        dependencies.Created(Group);
        dependencies.Created(Character);
        dependencies.Created(claimId);

        dependencies.GroupsProblem([Group]).ShouldBeNull();
        dependencies.CharacterProblem(Character).ShouldBeNull();
        dependencies.ClaimProblem(claimId).ShouldBeNull();
    }

    [Fact]
    public void Dependencies_ClaimChangeAlsoBlocksItsFields()
    {
        var claimId = new ClaimIdentification(ProjectId, 4);
        var plan = new ClaimRestorePlanViewModel(ProjectId, null, At, At,
            [new ClaimRestoreRowViewModel(claimId, ClaimRestoreActionView.Change, "Герой", null, null, [], null, [],
                ClaimStatusView.AddedByUser, ClaimStatusView.Approved, At, null, null, null, null, At, At, [])],
            []);

        new RestoreDependencies(plan).ClaimProblem(claimId).ShouldNotBeNull();
    }

    [Fact]
    public void ClaimEditor_OtherStatusChosen_NotPlannedStatus()
    {
        var row = new ClaimRestoreRowViewModel(new ClaimIdentification(ProjectId, 4), ClaimRestoreActionView.Change, "Герой", null, null, [], null, [],
            ClaimStatusView.AddedByUser, ClaimStatusView.Approved, At, null, null, null, null, At, At, []);
        var editor = new ClaimRestoreRowEditor(row);

        editor.AppliesPlannedStatus.ShouldBeTrue();
        editor.Status = null;
        editor.AppliesPlannedStatus.ShouldBeFalse();
    }

    [Fact]
    public void UploadedExport_DateFromInputAndBack()
    {
        var export = new UploadedExport(new MasterExportUploadViewModel("a.xlsx", MasterExportKindView.Claims, 1, null, 1, [], "p"));

        export.ToPlanExport().ShouldBeNull();
        export.LocalInputValue.ShouldBe("");

        export.SetFromLocalInput("2026-06-01T10:30");
        export.LocalInputValue.ShouldBe("2026-06-01T10:30");
        export.ToPlanExport().ShouldNotBeNull().Payload.ShouldBe("p");

        export.SetFromLocalInput("");
        export.ExportedAt.ShouldBeNull();
    }
}
