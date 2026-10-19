using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Web.Claims;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.ClaimRestore;

public class ClaimRestorePanelTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly DateTimeOffset At = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly UserIdentification Master = new(2);
    private static readonly UserLinkViewModel Player = new(new UserIdentification(10), "Игрок", ViewMode.Show);

    private readonly FakeClient client = new();
    private readonly FakeCharactersClient charactersClient = new();
    private readonly BunitJSModuleInterop selectModule;

    public ClaimRestorePanelTest()
    {
        Services.AddSingleton<ICharactersClient>(charactersClient);
        Services.AddSingleton<IClaimRestoreClient>(client);
        Services.AddSingleton<IUriLocator<ClaimIdentification>>(new FakeLocator());
        Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeLocator());
        Services.AddSingleton<ICharacterUriLocator>(new FakeLocator());
        Services.AddSingleton<IUriLocator<CharacterGroupIdentification>>(new FakeLocator());
        JSInterop.Mode = JSRuntimeMode.Loose;
        // Выбор персонажа (SingleCharacterSelector) — bootstrap-select поверх JS-модуля.
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        selectModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = selectModule.SetupVoid("initBootstrapSelect", _ => true);
        _ = selectModule.SetupVoid("refreshBootstrapSelect", _ => true);
    }

    private static readonly CharacterIdentification Created = new(ProjectId, 777);

    private static ClaimRestoreRowViewModel Row(
        int claimId, ClaimRestoreActionView action, string characterName, CharacterIdentification? character = null,
        ClaimStatusView status = ClaimStatusView.Approved)
        => new(
            new ClaimIdentification(ProjectId, claimId),
            action,
            characterName,
            CurrentCharacter: null,
            Character: character is null ? null : new ClaimRestoreCharacterOption(character, characterName),
            CharacterCandidates: [],
            Player,
            [Player],
            CurrentStatus: action == ClaimRestoreActionView.Create ? null : ClaimStatusView.AddedByUser,
            status,
            At,
            CurrentResponsibleMaster: action == ClaimRestoreActionView.Create ? null : Master,
            Master,
            CurrentFee: null,
            ManualFee: null,
            At,
            At,
            []);

    private static ClaimRestorePlanViewModel Plan(params ClaimRestoreRowViewModel[] rows)
        => new(ProjectId, null, At.AddDays(-30), At.AddDays(30), rows, [new ClaimRestoreMasterOption(Master, "Мастер")]);

    [Fact]
    public void ProjectNotBlocked_ShowsExplanation()
    {
        client.Plans.Enqueue(new ClaimRestorePlanViewModel(ProjectId, ClaimRestoreUnavailableReason.ProjectNotBlocked, At, At, [], []));

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Markup.ShouldContain("только пока проект заблокирован");
        cut.FindAll("table").ShouldBeEmpty();
    }

    [Fact]
    public void Apply_GoesInPlanOrderAndReusesCreatedCharacter()
    {
        client.Plans.Enqueue(Plan(
            Row(1, ClaimRestoreActionView.Nothing, "Ничего"),
            Row(2, ClaimRestoreActionView.Create, "Новый", status: ClaimStatusView.Discussed),
            Row(3, ClaimRestoreActionView.Create, "Новый", status: ClaimStatusView.Discussed)));
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        var checkboxes = cut.FindAll("tbody input[type=checkbox]:not(.new-character-slot)");
        checkboxes.Count.ShouldBe(3);
        checkboxes[0].HasAttribute("disabled").ShouldBeTrue();
        checkboxes[1].HasAttribute("checked").ShouldBeTrue();

        cut.Find("button[type=button]").Click();

        cut.WaitForAssertion(() => client.Applied.Count.ShouldBe(2));
        client.Applied[0].ClaimId.ClaimId.ShouldBe(2);
        client.Applied[0].NewCharacterName.ShouldBe("Новый");
        client.Applied[1].ClaimId.ClaimId.ShouldBe(3);
        client.Applied[1].Character.ShouldBe(new CharacterIdentification(ProjectId, 777));
        client.Applied[1].NewCharacterName.ShouldBeNull();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Применено строк: 2"));
    }

    [Fact]
    public void FailedAfterCreatingCharacter_SameNameSkipped_RetryGoesToFoundCharacter()
    {
        client.Plans.Enqueue(Plan(
            Row(2, ClaimRestoreActionView.Create, "Новый"),
            Row(3, ClaimRestoreActionView.Create, "Новый")));
        // Персонаж успел создаться, но строка упала — новый план находит его по имени.
        client.Plans.Enqueue(Plan(
            Row(2, ClaimRestoreActionView.Create, "Новый", Created),
            Row(3, ClaimRestoreActionView.Create, "Новый", Created)));
        client.Plans.Enqueue(Plan());
        client.FailOnce.Add(2);

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Find("button[type=button]").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось применить: 2"));
        client.Applied.Select(r => r.ClaimId.ClaimId).ShouldBe([2]);
        cut.Markup.ShouldContain("мог быть создан строкой выше");

        cut.Find("button[type=button]").Click();
        cut.WaitForAssertion(() => client.Applied.Count.ShouldBe(3));
        client.Applied[1].Character.ShouldBe(Created);
        client.Applied[1].NewCharacterName.ShouldBeNull();
        client.Applied[2].Character.ShouldBe(Created);
        client.Applied[2].NewCharacterName.ShouldBeNull();
    }

    [Fact]
    public void CreatedCharacterRememberedBetweenRuns()
    {
        client.Plans.Enqueue(Plan(
            Row(2, ClaimRestoreActionView.Create, "Новый", status: ClaimStatusView.Discussed),
            Row(3, ClaimRestoreActionView.Create, "Новый", status: ClaimStatusView.Discussed)));
        // План персонажа не нашёл (например, имя записано полем) — помогает память страницы.
        client.Plans.Enqueue(Plan(Row(3, ClaimRestoreActionView.Create, "Новый", status: ClaimStatusView.Discussed)));
        client.Plans.Enqueue(Plan());
        client.FailOnce.Add(3);

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Find("button[type=button]").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось применить: 1"));

        cut.Find("button[type=button]").Click();
        cut.WaitForAssertion(() => client.Applied.Count.ShouldBe(3));
        client.Applied[2].ClaimId.ClaimId.ShouldBe(3);
        client.Applied[2].Character.ShouldBe(Created);
        client.Applied[2].NewCharacterName.ShouldBeNull();
    }

    [Fact]
    public void UncheckedRowStaysUncheckedAfterApply()
    {
        client.Plans.Enqueue(Plan(
            Row(2, ClaimRestoreActionView.Change, "А", Created),
            Row(3, ClaimRestoreActionView.Change, "Б", Created)));
        client.Plans.Enqueue(Plan(Row(3, ClaimRestoreActionView.Change, "Б", Created)));

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));
        cut.FindAll("tbody input[type=checkbox]")[1].Change(false);

        cut.Find("button[type=button]").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Применено строк: 1"));
        client.Applied.Select(r => r.ClaimId.ClaimId).ShouldBe([2]);
        cut.Find("tbody input[type=checkbox]").HasAttribute("checked").ShouldBeFalse();
    }

    [Fact]
    public void ReloadFails_ShowsBothSummaryAndError()
    {
        client.Plans.Enqueue(Plan(Row(2, ClaimRestoreActionView.Change, "А", Created)));

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));
        cut.Find("button[type=button]").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось обновить план"));
        cut.Markup.ShouldContain("Применено строк: 1.");
        cut.FindAll(".alert-success").ShouldBeEmpty();
    }

    private static readonly CharacterGroupIdentification LostGroup = new(ProjectId, 50);
    private static readonly CharacterIdentification LostCharacter = new(ProjectId, 900);
    private static readonly ProjectFieldIdentification Field = new(ProjectId, 7);

    private static FieldRestoreChangeView FieldChange(string? value = "новое")
        => new(Field, "Легенда", value, "", value ?? "", At, MasterExportKindView.Characters, ApplyByDefault: true, [], []);

    private static CharacterRestoreRowViewModel CharacterRow(CharacterIdentification id, ClaimRestoreActionView action)
        => new(
            id, action, "Потерянный", action == ClaimRestoreActionView.Create ? null : "Потерянный",
            Groups: [new RestoreGroupOption(LostGroup, "Потерянная группа")],
            CurrentGroups: [],
            GroupsApplyByDefault: true,
            [FieldChange()],
            At,
            ApplyByDefault: true,
            []);

    /// <summary>План по выгрузкам: группа → новый персонаж в ней → заявка на него → изменение → поля заявки.</summary>
    private static ClaimRestorePlanViewModel ExportPlan(bool canEditRoles = true)
        => Plan(Row(3, ClaimRestoreActionView.Create, "Потерянный", LostCharacter)) with
        {
            CanEditRoles = canEditRoles,
            Groups = [new GroupRestoreRowViewModel(LostGroup, ClaimRestoreActionView.Create, "Потерянная группа", null, At, true, [])],
            CharacterCreations = [CharacterRow(LostCharacter, ClaimRestoreActionView.Create) with { ExportResponsibleMaster = Master }],
            CharacterChanges = [CharacterRow(Created, ClaimRestoreActionView.Change)],
            ClaimFields = [new ClaimFieldsRestoreRowViewModel(new ClaimIdentification(ProjectId, 3), "Потерянный", [FieldChange()], At, true, [], ExistsInDatabase: false)],
        };

    [Fact]
    public void ExportPlan_AppliedInPlanOrder()
    {
        client.Plans.Enqueue(ExportPlan());
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));
        cut.FindAll("button[type=button]").Last().Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Применено строк: 5"));
        client.Log.Select(r => r.GetType()).ShouldBe([
            typeof(GroupRestoreApplyRequest),
            typeof(CharacterRestoreApplyRequest),
            typeof(ClaimRestoreApplyRequest),
            typeof(CharacterRestoreApplyRequest),
            typeof(ClaimFieldsRestoreApplyRequest),
        ]);
        var creation = (CharacterRestoreApplyRequest)client.Log[1];
        creation.ShouldBe(new CharacterRestoreApplyRequest(LostCharacter, true, "Потерянный", [LostGroup], [new RestoreFieldValueRequest(Field, "новое")]), new CharacterRestoreApplyRequestComparer());
        ((CharacterRestoreApplyRequest)client.Log[3]).Create.ShouldBeFalse();
        // Заявка подаётся на создаваемого планом персонажа, а не на «нового персонажа».
        var claim = (ClaimRestoreApplyRequest)client.Log[2];
        claim.Character.ShouldBe(LostCharacter);
        claim.NewCharacterName.ShouldBeNull();
    }

    [Fact]
    public void ExportPlan_ClaimOnPlannedCharacter_ShownWithoutPicker()
    {
        client.Plans.Enqueue(ExportPlan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Find("table.restore-claims .planned-character").TextContent.ShouldBe("будет создан: Потерянный (№ 900)");
        cut.FindAll("table.restore-claims select.selectpicker").ShouldBeEmpty();
        cut.Find(".export-responsible").TextContent.ShouldContain("ответственный по выгрузке: Мастер — назначьте после восстановления");
    }

    /// <summary>Мастер видит, что персонаж восстановится шаблоном, а не персонажем игрока (#5645).</summary>
    [Theory]
    [InlineData(CharacterType.Slot, true, "Горячий шаблон")]
    [InlineData(CharacterType.Slot, false, "Шаблон")]
    [InlineData(CharacterType.NonPlayer, false, "NPC")]
    [InlineData(CharacterType.Player, true, "Горячая вакансия")]
    public void ExportPlan_CreationOfNotPlainCharacter_ShowsType(CharacterType type, bool isHot, string expected)
    {
        client.Plans.Enqueue(ExportPlan() with
        {
            CharacterCreations = [CharacterRow(LostCharacter, ClaimRestoreActionView.Create) with { CharacterType = type, IsHot = isHot }],
        });

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Find(".new-character-type").TextContent.ShouldBe($"будет создан как: {expected}");
    }

    [Fact]
    public void ExportPlan_CreationOfPlayerCharacter_NoTypeNote()
    {
        client.Plans.Enqueue(ExportPlan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.FindAll(".new-character-type").ShouldBeEmpty();
    }

    [Fact]
    public void ExportPlan_ClaimRowNotApplied_ClaimFieldsBlocked()
    {
        client.Plans.Enqueue(ExportPlan());
        // После применения поля заявки остались в плане — причина отказа видна в их строке.
        client.Plans.Enqueue(ExportPlan() with { Groups = [], CharacterCreations = [], CharacterChanges = [] });

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));
        cut.Find("table.restore-claims tbody input[type=checkbox]").Change(false);
        cut.FindAll("button[type=button]").Last().Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось применить: 1"));
        client.Log.ShouldNotContain(r => r is ClaimFieldsRestoreApplyRequest);
        cut.Markup.ShouldContain("поля не пишем, чтобы не затереть персонажа");
    }

    [Fact]
    public void ReloadFailed_PlanStale_ApplyDisabled()
    {
        client.Plans.Enqueue(Plan(Row(2, ClaimRestoreActionView.Change, "А", Created)));
        client.Uploads.Enqueue(new MasterExportUploadResult(
            new MasterExportUploadViewModel("roles.xlsx", MasterExportKindView.Characters, ProjectId.Value, At, 1, [], "p"),
            null));

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));
        cut.Find("button[type=button]").HasAttribute("disabled").ShouldBeFalse();

        // План по новой выгрузке построить не удалось — на экране план без неё.
        cut.FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>()
            .UploadFiles(InputFileContent.CreateFromText("x", "roles.xlsx"));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("План на экране устарел"));
        cut.FindAll("button[type=button]").Last().HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void ParallelReloads_OnlyLatestShown()
    {
        client.Plans.Enqueue(Plan());
        client.Plans.Enqueue(Plan());
        client.Uploads.Enqueue(new MasterExportUploadResult(
            new MasterExportUploadViewModel("roles.xlsx", MasterExportKindView.Characters, ProjectId.Value, null, 1, [], "p"),
            null));
        var first = new TaskCompletionSource<ClaimRestorePlanViewModel>();
        var second = new TaskCompletionSource<ClaimRestorePlanViewModel>();

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));
        cut.FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>()
            .UploadFiles(InputFileContent.CreateFromText("x", "roles.xlsx"));
        cut.WaitForAssertion(() => client.PlanRequests.Count.ShouldBe(2));

        client.PendingPlans.Enqueue(first);
        client.PendingPlans.Enqueue(second);
        cut.Find("input.export-date").Change("2026-06-01T10:00");
        // Пока план строится, применять нельзя.
        cut.Find("input.export-date").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("input.export-date").Change("2026-06-02T10:00");
        cut.WaitForAssertion(() => client.PlanRequests.Count.ShouldBe(4));

        cut.InvokeAsync(() => second.SetResult(Plan(Row(222, ClaimRestoreActionView.Change, "Свежий", Created))));
        cut.InvokeAsync(() => first.SetResult(Plan(Row(111, ClaimRestoreActionView.Change, "Устаревший", Created))));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("/claim/222"));
        cut.Markup.ShouldNotContain("/claim/111");
        cut.FindAll("button[type=button]").Last().HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void ExportPlan_FailedGroup_DependentRowsNotApplied()
    {
        client.Plans.Enqueue(ExportPlan());
        // После применения изменение персонажа ещё в плане — пояснение видно в его строке.
        client.Plans.Enqueue(ExportPlan() with { Rows = [], Groups = [], CharacterCreations = [], ClaimFields = [] });
        client.FailGroups.Add(LostGroup.CharacterGroupId);

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));
        cut.FindAll("button[type=button]").Last().Click();

        // Группа упала → персонаж в ней не создаётся → заявка на него и её поля тоже.
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Применено строк: 1. Не удалось применить: 4"));
        client.Log.Count.ShouldBe(2);
        _ = client.Log[0].ShouldBeOfType<GroupRestoreApplyRequest>();
        // Изменению существующего персонажа нужна та же группа — но поля от неё не зависят: пишем только их.
        var change = client.Log[1].ShouldBeOfType<CharacterRestoreApplyRequest>();
        change.Create.ShouldBeFalse();
        change.Groups.ShouldBeNull();
        change.Fields.ShouldHaveSingleItem().FieldId.ShouldBe(Field);
        cut.Markup.ShouldContain("Поля записаны, группы не изменены: не создана группа 50");
    }

    [Fact]
    public void NoExportsAndNothingByNotifications_SuggestsUploadingExports()
    {
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Markup.ShouldContain("По уведомлениям восстанавливать нечего. Загрузите выгрузки персонажей и заявок, если они есть.");
    }

    [Fact]
    public void FeePaidMismatch_ShowsBothSums()
    {
        client.Plans.Enqueue(Plan(
            Row(1, ClaimRestoreActionView.Nothing, "Гамлет") with
            {
                Warnings = [ClaimRestoreWarningView.FeePaidMismatch],
                FeePaidMismatch = new ClaimRestoreFeePaidMismatchView(Exported: 1500, InDatabase: 1000),
            }));

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Find("table.restore-claims").TextContent.ShouldContain("Уплачено в выгрузке (1500) не совпадает с оплатами в базе (1000)");
    }

    [Fact]
    public void RowsWithNothingToDo_NotCounted()
    {
        client.Plans.Enqueue(Plan(
            Row(1, ClaimRestoreActionView.Nothing, "Ничего"),
            Row(2, ClaimRestoreActionView.Change, "А", Created)));

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Markup.ShouldContain("Строк в плане: 1, из них отмечено: 1.");
    }

    [Fact]
    public void ClaimsSection_NotCreatedClaimNumberIsNotALink()
    {
        client.Plans.Enqueue(Plan(
            Row(1, ClaimRestoreActionView.Nothing, "Ничего"),
            Row(2, ClaimRestoreActionView.Change, "А", Created),
            Row(3, ClaimRestoreActionView.Create, "Новый")));

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        var cells = cut.FindAll("table.restore-claims tbody td:nth-child(2)");
        cells.Count.ShouldBe(3);
        // Уже созданные заявки (даже «ничего») — ссылки; «создать» — ещё нет в базе, просто текст.
        cells[0].QuerySelector("a").ShouldNotBeNull();
        cells[1].QuerySelector("a").ShouldNotBeNull();
        cells[2].QuerySelector("a").ShouldBeNull();
        cells[2].TextContent.Trim().ShouldBe("3");
    }

    [Fact]
    public void ClaimFieldsSection_LinkOnlyForExistingClaims()
    {
        client.Plans.Enqueue(Plan() with
        {
            ClaimFields =
            [
                new ClaimFieldsRestoreRowViewModel(new ClaimIdentification(ProjectId, 5), "Старый", [FieldChange()], At, true, [], ExistsInDatabase: true),
                new ClaimFieldsRestoreRowViewModel(new ClaimIdentification(ProjectId, 3), "Потерянный", [FieldChange()], At, true, [], ExistsInDatabase: false),
            ],
        });

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        var cells = cut.FindAll("table.restore-claim-fields tbody td:nth-child(2)");
        cells.Count.ShouldBe(2);
        cells[0].QuerySelector("a").ShouldNotBeNull();
        cells[1].QuerySelector("a").ShouldBeNull();
        cells[1].TextContent.Trim().ShouldBe("3");
    }

    [Fact]
    public void ExportPlan_WithoutEditRoles_GroupsAndCharactersShownButNotApplied()
    {
        client.Plans.Enqueue(ExportPlan(canEditRoles: false) with { Rows = [] });
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Markup.ShouldContain("правом редактировать персонажей и группы");
        cut.FindAll("table.restore-groups tbody input[type=checkbox]").ShouldAllBe(c => c.HasAttribute("disabled"));
        cut.FindAll("table.restore-characters tbody input[type=checkbox]").ShouldAllBe(c => c.HasAttribute("disabled"));

        cut.FindAll("button[type=button]").Last().Click();
        // Поля заявки зависят от заявки 3, которой в этом плане нет, — применяются.
        cut.WaitForAssertion(() => client.Log.ShouldHaveSingleItem().ShouldBeOfType<ClaimFieldsRestoreApplyRequest>());
    }

    [Fact]
    public void UploadedXlsxWithoutDate_NeedsDateBeforeItGoesToPlan()
    {
        client.Plans.Enqueue(Plan());
        client.Plans.Enqueue(Plan());
        client.Plans.Enqueue(Plan());
        // В свойствах книги даты нет — её вводит мастер.
        client.Uploads.Enqueue(new MasterExportUploadResult(
            new MasterExportUploadViewModel("roles.xlsx", MasterExportKindView.Characters, ProjectId.Value,
                SuggestedDate: null, RowCount: 10, [MasterExportWarningView.TruncatedValues], "payload"),
            null));

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));
        cut.FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>()
            .UploadFiles(InputFileContent.CreateFromText("x", "roles.xlsx"));

        cut.WaitForAssertion(() => client.PlanRequests.Count.ShouldBe(2));
        client.PlanRequests[1].Exports.ShouldBeEmpty();
        cut.Markup.ShouldContain("без неё выгрузка не учитывается");
        cut.Markup.ShouldContain("Excel обрезал слишком длинные значения");

        cut.Find("input.export-date").Change("2026-06-01T10:00");

        cut.WaitForAssertion(() => client.PlanRequests.Count.ShouldBe(3));
        client.PlanRequests[2].Exports.ShouldHaveSingleItem().Payload.ShouldBe("payload");
        cut.Markup.ShouldNotContain("без неё выгрузка не учитывается");
    }

    [Fact]
    public void UploadRejected_ShowsReason()
    {
        client.Plans.Enqueue(Plan());
        client.Plans.Enqueue(Plan());
        client.Uploads.Enqueue(new MasterExportUploadResult(null, MasterExportUploadErrorView.UnknownExportType));

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));
        cut.FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>()
            .UploadFiles(InputFileContent.CreateFromText("x", "finance.xlsx"));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("finance.xlsx: Это не выгрузка списка персонажей или заявок"));
    }

    /// <summary>Списки в record сравниваются по ссылке — сравниваем по содержимому.</summary>
    private sealed class CharacterRestoreApplyRequestComparer : IEqualityComparer<CharacterRestoreApplyRequest>
    {
        public bool Equals(CharacterRestoreApplyRequest? x, CharacterRestoreApplyRequest? y)
            => x is not null && y is not null
                && x.CharacterId == y.CharacterId && x.Create == y.Create && x.Name == y.Name
                && x.CharacterType == y.CharacterType && x.IsHot == y.IsHot
                && (x.Groups ?? []).SequenceEqual(y.Groups ?? []) && x.Fields.SequenceEqual(y.Fields);

        public int GetHashCode(CharacterRestoreApplyRequest obj) => obj.CharacterId.GetHashCode();
    }

    private sealed class FakeClient : IClaimRestoreClient
    {
        public Queue<ClaimRestorePlanViewModel> Plans { get; } = new();
        public List<ClaimRestoreApplyRequest> Applied { get; } = [];

        /// <summary>Заявки, на которых Apply один раз упадёт — уже после того, как «создал» персонажа.</summary>
        public HashSet<int> FailOnce { get; } = [];

        public List<ClaimRestorePlanRequest> PlanRequests { get; } = [];

        public Queue<MasterExportUploadResult> Uploads { get; } = new();

        /// <summary>Все применения по порядку, всех видов.</summary>
        public List<object> Log { get; } = [];

        /// <summary>Группы, применение которых упадёт.</summary>
        public HashSet<int> FailGroups { get; } = [];

        /// <summary>Ответы на запрос плана, которые тест завершит сам; берутся раньше <see cref="Plans"/>.</summary>
        public Queue<TaskCompletionSource<ClaimRestorePlanViewModel>> PendingPlans { get; } = new();

        public Task<ClaimRestorePlanViewModel> GetPlan(ClaimRestorePlanRequest request)
        {
            PlanRequests.Add(request);
            if (PendingPlans.TryDequeue(out var pending))
            {
                return pending.Task;
            }
            return Plans.TryDequeue(out var plan) ? Task.FromResult(plan) : throw new InvalidOperationException("нет связи");
        }

        public Task<MasterExportUploadResult> ParseExport(ProjectIdentification projectId, string fileName, Stream content)
            => Task.FromResult(Uploads.Dequeue());

        public Task ApplyGroup(GroupRestoreApplyRequest request)
        {
            Log.Add(request);
            return FailGroups.Contains(request.GroupId.CharacterGroupId)
                ? throw new InvalidOperationException("Сбой группы")
                : Task.CompletedTask;
        }

        public Task ApplyCharacter(CharacterRestoreApplyRequest request)
        {
            Log.Add(request);
            return Task.CompletedTask;
        }

        public Task ApplyClaimFields(ClaimFieldsRestoreApplyRequest request)
        {
            Log.Add(request);
            return Task.CompletedTask;
        }

        public Task<ClaimRestoreApplyResult> Apply(ClaimRestoreApplyRequest request)
        {
            Log.Add(request);
            Applied.Add(request);
            if (FailOnce.Remove(request.ClaimId.ClaimId))
            {
                throw new InvalidOperationException("Сбой после создания персонажа");
            }
            return Task.FromResult(new ClaimRestoreApplyResult(request.Character ?? Created));
        }
    }

    [Fact]
    public void Create_CharacterChosenInSharedPicker_SentInsteadOfNew()
    {
        client.Plans.Enqueue(Plan(Row(2, ClaimRestoreActionView.Create, "Новый") with
        {
            CharacterCandidates = [new ClaimRestoreCharacterOption(Hero, "Новый"), new ClaimRestoreCharacterOption(Twin, "Новый")],
        }));
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        var picker = cut.Find("select.selectpicker");
        var options = picker.QuerySelectorAll("option");
        options[0].TextContent.ShouldBe("Новый персонаж");
        options[0].HasAttribute("selected").ShouldBeTrue();
        cut.Markup.ShouldContain("подходят по имени");
        cut.FindAll("input[placeholder='Имя нового персонажа']").ShouldHaveSingleItem();
        // План ищет персонажей только среди активных — и выбор должен предлагать только их,
        // иначе удалённые тёзки появляются вторым экземпляром.
        charactersClient.RequestedListTypes.ShouldAllBe(t => t == CharacterListType.Active);
        charactersClient.RequestedListTypes.ShouldNotBeEmpty();

        var twinValue = options.Single(o => o.TextContent.Contains("Близнец")).GetAttribute("value")!;
        selectModule.Setup<List<string>>("getSelectedValues", _ => true).SetResult([twinValue]);
        cut.Find("select.selectpicker").Change(twinValue);

        cut.FindAll("input[placeholder='Имя нового персонажа']").ShouldBeEmpty();
        cut.FindAll("button[type=button]").Last().Click();

        cut.WaitForAssertion(() => client.Applied.ShouldHaveSingleItem());
        client.Applied[0].Character.ShouldBe(Twin);
        client.Applied[0].NewCharacterName.ShouldBeNull();
    }

    private static readonly CharacterIdentification Hero = new(ProjectId, 100);
    private static readonly CharacterIdentification Twin = new(ProjectId, 101);

    [Fact]
    public void MixedSlotChoiceInOneName_LaterRowGoesToCreatedSlot()
    {
        // Первая строка создаёт шаблон, у второй галочка «Шаблон» снята: персонаж с этим именем уже есть —
        // строка идёт на него (домен сам создаст персонажа из шаблона).
        client.Plans.Enqueue(Plan(
            Row(2, ClaimRestoreActionView.Create, "Стражник") with { NewCharacterIsSlot = true },
            Row(3, ClaimRestoreActionView.Create, "Стражник")));
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        var slots = cut.FindAll("input.new-character-slot");
        slots[0].HasAttribute("checked").ShouldBeTrue();
        slots[1].HasAttribute("checked").ShouldBeFalse();

        cut.Find("button[type=button]").Click();

        cut.WaitForAssertion(() => client.Applied.Count.ShouldBe(2));
        client.Applied[0].NewCharacterIsSlot.ShouldBeTrue();
        client.Applied[1].Character.ShouldBe(Created);
        client.Applied[1].NewCharacterName.ShouldBeNull();
        client.Applied[1].NewCharacterIsSlot.ShouldBeFalse();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Применено строк: 2"));
    }

    [Fact]
    public void TwoApprovedOnSameNewPlayerCharacter_SecondGoesToCreated()
    {
        // Персонаж создан первой строкой — вторая идёт на него; дубль принятой отклонит домен.
        client.Plans.Enqueue(Plan(
            Row(2, ClaimRestoreActionView.Create, "Стражник"),
            Row(3, ClaimRestoreActionView.Create, "Стражник")));
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.Find("button[type=button]").Click();

        cut.WaitForAssertion(() => client.Applied.Count.ShouldBe(2));
        client.Applied[1].Character.ShouldBe(Created);
    }

    [Fact]
    public void LooksLikeSlot_ApprovedRowsGoToOneSlot()
    {
        client.Plans.Enqueue(Plan(
            Row(2, ClaimRestoreActionView.Create, "Стражник") with { NewCharacterIsSlot = true, Warnings = [ClaimRestoreWarningView.LooksLikeSlot] },
            Row(3, ClaimRestoreActionView.Create, "Стражник") with { NewCharacterIsSlot = true, Warnings = [ClaimRestoreWarningView.LooksLikeSlot] }));
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        cut.FindAll("input.new-character-slot").ShouldAllBe(c => c.HasAttribute("checked"));
        cut.FindAll(".text-info").ShouldAllBe(e => e.TextContent.StartsWith("Похоже на шаблон"));
        cut.FindAll(".text-info").Count.ShouldBe(2);

        cut.Find("button[type=button]").Click();

        cut.WaitForAssertion(() => client.Applied.Count.ShouldBe(2));
        client.Applied[0].NewCharacterName.ShouldBe("Стражник");
        client.Applied[0].NewCharacterIsSlot.ShouldBeTrue();
        // Шаблон, который вернул сервер, — и вторая принятая заявка идёт на него: персонажа ей создаст домен.
        client.Applied[1].Character.ShouldBe(Created);
        client.Applied[1].NewCharacterIsSlot.ShouldBeFalse();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Применено строк: 2"));
    }

    [Fact]
    public void SlotCheckbox_SendsNewCharacterAsSlot()
    {
        client.Plans.Enqueue(Plan(Row(2, ClaimRestoreActionView.Create, "Стражник")));
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        var slot = cut.Find("input.new-character-slot");
        slot.HasAttribute("checked").ShouldBeFalse();
        slot.Change(true);
        cut.FindAll("button[type=button]").Last().Click();

        cut.WaitForAssertion(() => client.Applied.ShouldHaveSingleItem().NewCharacterIsSlot.ShouldBeTrue());
    }

    private sealed class FakeCharactersClient : ICharactersClient
    {
        public List<CharacterListType> RequestedListTypes { get; } = [];

        public Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType = CharacterListType.All)
        {
            RequestedListTypes.Add(listType);
            return Task.FromResult<List<CharacterDto>>(
            [
                new CharacterDto(Hero, "Новый", "", IsPublic: true),
                new CharacterDto(Twin, "Новый (Близнец)", "", IsPublic: true),
            ]);
        }
    }

    private sealed class FakeLocator : IUriLocator<ClaimIdentification>, IUriLocator<UserLinkViewModel>, ICharacterUriLocator, IUriLocator<CharacterGroupIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new($"/claim/{target.ClaimId}", UriKind.Relative);
        public Uri GetUri(CharacterGroupIdentification target) => new($"/group/{target.CharacterGroupId}", UriKind.Relative);
        public Uri GetUri(UserLinkViewModel target) => new("/user", UriKind.Relative);
        public Uri GetDetailsUri(CharacterIdentification characterId) => new("/character", UriKind.Relative);
        public Uri GetAddClaimUri(CharacterIdentification characterId) => new("/character", UriKind.Relative);
        public Uri GetEditUri(CharacterIdentification characterId) => new("/character", UriKind.Relative);
    }
}
