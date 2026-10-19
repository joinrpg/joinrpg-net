using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.Claims;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.ClaimRestore;

public class ClaimRestoreRowEditorTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly DateTimeOffset FirstActivity = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly UserIdentification Master = new(2);
    private static readonly UserLinkViewModel Player = new(new UserIdentification(10), "Игрок", ViewMode.Show);
    private static readonly UserLinkViewModel OtherPlayer = new(new UserIdentification(11), "Другой игрок", ViewMode.Show);
    private static readonly ClaimRestoreCharacterOption Hero = new(new CharacterIdentification(ProjectId, 100), "Герой");
    private static readonly ClaimRestoreCharacterOption HeroTwin = new(new CharacterIdentification(ProjectId, 101), "Герой");

    private static ClaimRestoreRowViewModel Row(
        ClaimRestoreActionView action = ClaimRestoreActionView.Create,
        string? characterName = "Герой",
        ClaimRestoreCharacterOption? character = null,
        IReadOnlyList<ClaimRestoreCharacterOption>? candidates = null,
        UserLinkViewModel? player = null,
        IReadOnlyList<UserLinkViewModel>? playerCandidates = null,
        UserIdentification? responsible = null,
        int claimId = 500,
        IReadOnlyList<ClaimRestoreWarningView>? warnings = null)
        => new(
            new ClaimIdentification(ProjectId, claimId),
            action,
            characterName,
            CurrentCharacter: null,
            character,
            candidates ?? [],
            player,
            playerCandidates ?? (player is null ? [] : [player]),
            CurrentStatus: null,
            ClaimStatusView.Approved,
            FirstActivity.AddDays(1),
            CurrentResponsibleMaster: null,
            responsible,
            CurrentFee: null,
            ManualFee: 1000,
            FirstActivity,
            FirstActivity.AddDays(2),
            warnings ?? []);

    [Fact]
    public void Create_EverythingKnown_SelectedAndMapped()
    {
        var editor = new ClaimRestoreRowEditor(Row(character: Hero, player: Player, responsible: Master));

        editor.BlockingProblem.ShouldBeNull();
        editor.Selected.ShouldBeTrue();
        editor.ToApplyRequest(new Dictionary<string, CharacterIdentification>()).ShouldBe(new ClaimRestoreApplyRequest(
            new ClaimIdentification(ProjectId, 500),
            ClaimRestoreActionView.Create,
            Hero.CharacterId,
            NewCharacterName: null,
            Player.UserId,
            PlayerLink: null,
            ClaimStatusView.Approved,
            FirstActivity.AddDays(1),
            Master,
            1000,
            FirstActivity));
    }

    [Fact]
    public void Create_AmbiguousCharacter_NewByDefaultButNotSelected()
    {
        var editor = new ClaimRestoreRowEditor(Row(
            candidates: [Hero, HeroTwin], player: Player, responsible: Master,
            warnings: [ClaimRestoreWarningView.CharacterAmbiguous]));

        // «Не выбрано» у выбора персонажа нет: без выбора — новый персонаж, но строку мастер проверяет сам.
        editor.NewCharacter.ShouldBeTrue();
        editor.BlockingProblem.ShouldBeNull();
        editor.Selected.ShouldBeFalse();

        editor.Character = HeroTwin.CharacterId;

        editor.NewCharacter.ShouldBeFalse();
        var request = editor.ToApplyRequest(new Dictionary<string, CharacterIdentification>());
        request.Character.ShouldBe(HeroTwin.CharacterId);
        request.NewCharacterName.ShouldBeNull();
    }

    [Fact]
    public void Create_SwitchBackToNewCharacter()
    {
        var editor = new ClaimRestoreRowEditor(Row(character: Hero, player: Player, responsible: Master));
        editor.NewCharacter.ShouldBeFalse();

        editor.Character = null;

        editor.NewCharacter.ShouldBeTrue();
        var request = editor.ToApplyRequest(new Dictionary<string, CharacterIdentification>());
        request.Character.ShouldBeNull();
        request.NewCharacterName.ShouldBe("Герой");
    }

    [Fact]
    public void Create_NoCharacterName_BlockedUntilNamed()
    {
        var editor = new ClaimRestoreRowEditor(Row(characterName: null, player: Player, responsible: Master));

        editor.NewCharacter.ShouldBeTrue();
        editor.BlockingProblem.ShouldNotBeNull();

        editor.NewCharacterName = "Новое имя";

        editor.BlockingProblem.ShouldBeNull();
    }

    [Fact]
    public void Create_PlayerAmbiguous_BlockedUntilChosen()
    {
        var editor = new ClaimRestoreRowEditor(Row(character: Hero, playerCandidates: [Player, OtherPlayer], responsible: Master));

        editor.ManualPlayer.ShouldBeFalse();
        editor.BlockingProblem.ShouldNotBeNull();

        editor.Player = OtherPlayer.UserId;

        editor.BlockingProblem.ShouldBeNull();
    }

    [Fact]
    public void Create_PlayerUnknown_TakenFromLink()
    {
        var editor = new ClaimRestoreRowEditor(Row(character: Hero, responsible: Master));

        editor.ManualPlayer.ShouldBeTrue();
        editor.BlockingProblem.ShouldNotBeNull();

        editor.PlayerLinks = ["https://joinrpg.ru/user/42"];

        editor.BlockingProblem.ShouldBeNull();
        var request = editor.ToApplyRequest(new Dictionary<string, CharacterIdentification>());
        request.Player.ShouldBeNull();
        request.PlayerLink.ShouldBe("https://joinrpg.ru/user/42");
    }

    [Fact]
    public void Create_SameNewCharacterTwice_SecondGoesToCreated()
    {
        var first = new ClaimRestoreRowEditor(Row(player: Player, responsible: Master, claimId: 500));
        var second = new ClaimRestoreRowEditor(Row(characterName: " Герой ", player: OtherPlayer, responsible: Master, claimId: 501));
        var created = new Dictionary<string, CharacterIdentification>();

        first.CreatesCharacterNamed(created).ShouldBe("Герой");
        var firstRequest = first.ToApplyRequest(created);
        firstRequest.Character.ShouldBeNull();
        firstRequest.NewCharacterName.ShouldBe("Герой");

        created["Герой"] = Hero.CharacterId;

        second.CreatesCharacterNamed(created).ShouldBeNull();
        var secondRequest = second.ToApplyRequest(created);
        secondRequest.Character.ShouldBe(Hero.CharacterId);
        secondRequest.NewCharacterName.ShouldBeNull();
    }

    [Fact]
    public void Nothing_NotSelectedAndNotApplicable()
    {
        var editor = new ClaimRestoreRowEditor(Row(action: ClaimRestoreActionView.Nothing, player: Player));

        editor.CanApply.ShouldBeFalse();
        editor.Selected.ShouldBeFalse();
    }

    [Fact]
    public void Change_KeepsMoveTargetAndResponsible()
    {
        var editor = new ClaimRestoreRowEditor(Row(action: ClaimRestoreActionView.Change, character: Hero, player: Player, responsible: Master));

        editor.Selected.ShouldBeTrue();
        var request = editor.ToApplyRequest(new Dictionary<string, CharacterIdentification>());
        request.Character.ShouldBe(Hero.CharacterId);
        request.ResponsibleMaster.ShouldBe(Master);
        request.Player.ShouldBeNull();
        request.NewCharacterName.ShouldBeNull();
    }

    [Fact]
    public void CopyEditsFrom_FailedRow_KeepsMasterEdits()
    {
        var failed = new ClaimRestoreRowEditor(Row(candidates: [Hero, HeroTwin], player: Player, responsible: Master))
        {
            Character = HeroTwin.CharacterId,
            Selected = true,
            Succeeded = false,
            Error = "Ошибка",
        };
        var reloaded = new ClaimRestoreRowEditor(Row(candidates: [Hero, HeroTwin], player: Player, responsible: Master));

        reloaded.CopyEditsFrom(failed);

        reloaded.Character.ShouldBe(HeroTwin.CharacterId);
        reloaded.Selected.ShouldBeTrue();
        reloaded.Error.ShouldBe("Ошибка");
    }

    [Theory]
    [InlineData(ClaimRestoreWarningView.LostClaimNotSubmittedAfterBackup)]
    [InlineData(ClaimRestoreWarningView.CharacterRenamed)]
    [InlineData(ClaimRestoreWarningView.StatusUnknown)]
    [InlineData(ClaimRestoreWarningView.ResponsibleNotFound)]
    public void RowWithWarning_NotSelectedByDefault(ClaimRestoreWarningView warning)
    {
        var editor = new ClaimRestoreRowEditor(Row(character: Hero, player: Player, responsible: Master, warnings: [warning]));

        editor.BlockingProblem.ShouldBeNull();
        editor.Selected.ShouldBeFalse();
    }

    [Fact]
    public void CopyEditsFrom_NotApplied_KeepsUncheckAndEdits()
    {
        var previous = new ClaimRestoreRowEditor(Row(action: ClaimRestoreActionView.Change, character: Hero, player: Player, responsible: Master))
        {
            Selected = false,
            ResponsibleMaster = new UserIdentification(3),
        };
        var reloaded = new ClaimRestoreRowEditor(Row(action: ClaimRestoreActionView.Change, character: Hero, player: Player, responsible: Master));

        reloaded.CopyEditsFrom(previous);

        reloaded.Selected.ShouldBeFalse();
        reloaded.ResponsibleMaster.ShouldBe(new UserIdentification(3));
        reloaded.Succeeded.ShouldBeNull();
    }

    [Fact]
    public void CopyEditsFrom_PlanNowFindsCharacter_DoesNotCreateAnother()
    {
        var failed = new ClaimRestoreRowEditor(Row(player: Player, responsible: Master))
        {
            Succeeded = false,
            Error = "Ошибка",
        };
        failed.NewCharacter.ShouldBeTrue();
        // Персонаж успел создаться — новый план находит его по имени.
        var reloaded = new ClaimRestoreRowEditor(Row(character: Hero, player: Player, responsible: Master));

        reloaded.CopyEditsFrom(failed);

        reloaded.NewCharacter.ShouldBeFalse();
        reloaded.Character.ShouldBe(Hero.CharacterId);
        var request = reloaded.ToApplyRequest(new Dictionary<string, CharacterIdentification>());
        request.Character.ShouldBe(Hero.CharacterId);
        request.NewCharacterName.ShouldBeNull();
    }

    [Fact]
    public void CopyEditsFrom_Succeeded_DoesNotCarryEdits()
    {
        var applied = new ClaimRestoreRowEditor(Row(action: ClaimRestoreActionView.Change, character: Hero, player: Player, responsible: Master))
        {
            Selected = false,
            Succeeded = true,
        };
        var reloaded = new ClaimRestoreRowEditor(Row(action: ClaimRestoreActionView.Change, character: Hero, player: Player, responsible: Master));

        reloaded.CopyEditsFrom(applied);

        reloaded.Succeeded.ShouldBe(true);
        reloaded.Selected.ShouldBeTrue();
    }

    [Fact]
    public void Create_ResponsibleUnknown_BlockedUntilChosen()
    {
        var editor = new ClaimRestoreRowEditor(Row(character: Hero, player: Player, responsible: null));

        editor.BlockingProblem.ShouldBe("Выберите ответственного мастера");
        editor.Selected.ShouldBeFalse();
    }

    [Theory]
    [InlineData(ClaimRestoreWarningView.CharacterFromSlot)]
    [InlineData(ClaimRestoreWarningView.LooksLikeSlot)]
    public void RowWithNote_SelectedByDefault(ClaimRestoreWarningView note)
    {
        var editor = new ClaimRestoreRowEditor(Row(character: Hero, player: Player, responsible: Master, warnings: [note]));

        note.IsNote().ShouldBeTrue();
        editor.Selected.ShouldBeTrue();
    }

    [Fact]
    public void Create_NewCharacterAsSlot_SentAsSlotOnlyWhenCreated()
    {
        var first = new ClaimRestoreRowEditor(Row(player: Player, responsible: Master, claimId: 500, warnings: [ClaimRestoreWarningView.LooksLikeSlot]) with { NewCharacterIsSlot = true });
        var second = new ClaimRestoreRowEditor(Row(player: OtherPlayer, responsible: Master, claimId: 501, warnings: [ClaimRestoreWarningView.LooksLikeSlot]) with { NewCharacterIsSlot = true });
        var created = new Dictionary<string, CharacterIdentification>();

        first.NewCharacterIsSlot.ShouldBeTrue();
        var firstRequest = first.ToApplyRequest(created);
        firstRequest.NewCharacterName.ShouldBe("Герой");
        firstRequest.NewCharacterIsSlot.ShouldBeTrue();

        // Для шаблона домен вернул сам шаблон — вторая принятая заявка подаётся на него же.
        created["Герой"] = Hero.CharacterId;
        var secondRequest = second.ToApplyRequest(created);
        secondRequest.Character.ShouldBe(Hero.CharacterId);
        secondRequest.NewCharacterName.ShouldBeNull();
        secondRequest.NewCharacterIsSlot.ShouldBeFalse();
    }

    [Fact]
    public void CopyEditsFrom_FailedRow_KeepsSlotChoice()
    {
        var failed = new ClaimRestoreRowEditor(Row(player: Player, responsible: Master))
        {
            NewCharacterIsSlot = true,
            Succeeded = false,
        };
        var reloaded = new ClaimRestoreRowEditor(Row(player: Player, responsible: Master));

        reloaded.CopyEditsFrom(failed);

        reloaded.NewCharacterIsSlot.ShouldBeTrue();
    }
}
