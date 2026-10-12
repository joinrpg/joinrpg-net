using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
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
        JSInterop.Mode = JSRuntimeMode.Loose;
        // Выбор персонажа (SingleCharacterSelector) — bootstrap-select поверх JS-модуля.
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        selectModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = selectModule.SetupVoid("initBootstrapSelect", _ => true);
        _ = selectModule.SetupVoid("refreshBootstrapSelect", _ => true);
    }

    private static readonly CharacterIdentification Created = new(ProjectId, 777);

    private static ClaimRestoreRowViewModel Row(int claimId, ClaimRestoreActionView action, string characterName, CharacterIdentification? character = null)
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
            ClaimStatusView.Approved,
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
            Row(2, ClaimRestoreActionView.Create, "Новый"),
            Row(3, ClaimRestoreActionView.Create, "Новый")));
        client.Plans.Enqueue(Plan());

        var cut = Render<ClaimRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

        var checkboxes = cut.FindAll("tbody input[type=checkbox]");
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
            Row(2, ClaimRestoreActionView.Create, "Новый"),
            Row(3, ClaimRestoreActionView.Create, "Новый")));
        // План персонажа не нашёл (например, имя записано полем) — помогает память страницы.
        client.Plans.Enqueue(Plan(Row(3, ClaimRestoreActionView.Create, "Новый")));
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

    private sealed class FakeClient : IClaimRestoreClient
    {
        public Queue<ClaimRestorePlanViewModel> Plans { get; } = new();
        public List<ClaimRestoreApplyRequest> Applied { get; } = [];

        /// <summary>Заявки, на которых Apply один раз упадёт — уже после того, как «создал» персонажа.</summary>
        public HashSet<int> FailOnce { get; } = [];

        public Task<ClaimRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
            => Plans.TryDequeue(out var plan) ? Task.FromResult(plan) : throw new InvalidOperationException("нет связи");

        public Task<ClaimRestoreApplyResult> Apply(ClaimRestoreApplyRequest request)
        {
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

    private sealed class FakeLocator : IUriLocator<ClaimIdentification>, IUriLocator<UserLinkViewModel>, ICharacterUriLocator
    {
        public Uri GetUri(ClaimIdentification target) => new($"/claim/{target.ClaimId}", UriKind.Relative);
        public Uri GetUri(UserLinkViewModel target) => new("/user", UriKind.Relative);
        public Uri GetDetailsUri(CharacterIdentification characterId) => new("/character", UriKind.Relative);
        public Uri GetAddClaimUri(CharacterIdentification characterId) => new("/character", UriKind.Relative);
        public Uri GetEditUri(CharacterIdentification characterId) => new("/character", UriKind.Relative);
    }
}
