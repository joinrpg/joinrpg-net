using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Условия автоприёма (<see cref="ClaimAutoAccept"/>): действует ли автоприём на заявку
/// и чего не хватает до его срабатывания.
/// </summary>
public class ClaimAutoAcceptTest
{
    private MockedProject Mock { get; } = new();

    /// <summary>
    /// Заявка на шаблон в проекте с заданными настройками. Согласие на защищённые данные — на
    /// уровне заявки, поэтому задаётся прямо в сущности мока (в БД колонка с опечаткой).
    /// </summary>
    private ClaimInfo MakeClaimInfo(
        bool autoAcceptEnabled,
        ClaimStatus claimStatus = ClaimStatus.AddedByUser,
        CharacterType characterType = CharacterType.Slot,
        int? slotLimit = 5,
        bool sensitiveDataRequired = false,
        bool playerAllowedSensitiveData = true)
    {
        Mock.Project.Details.AutoAcceptClaims = autoAcceptEnabled;
        Mock.ReInitProjectInfo();

        var projectInfo = Mock.ProjectInfo;
        if (sensitiveDataRequired)
        {
            projectInfo = projectInfo.WithProfileRequirementSettings(
                ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Required });
        }

        var character = characterType == CharacterType.Slot
            ? Mock.CreateSlot("Шаблон", slotLimit)
            : Mock.CreateCharacter("Персонаж");
        var claim = Mock.CreateClaim(character, Mock.Player);
        claim.ClaimStatus = claimStatus;
        claim.PlayerAllowedSenstiveData = playerAllowedSensitiveData;

        var characterInfo = Mock.GetCharacterInfo(character, projectInfo);
        return new ClaimInfo(new ClaimInCharacter(characterInfo, claim.GetId()), Mock.PlayerInfo);
    }

    [Theory]
    [InlineData(ClaimStatus.AddedByUser)]
    [InlineData(ClaimStatus.Discussed)]
    public void ActiveClaimOnSlotIsEligible(ClaimStatus status)
    {
        var claimInfo = MakeClaimInfo(autoAcceptEnabled: true, claimStatus: status);

        ClaimAutoAccept.AppliesTo(claimInfo).ShouldBeTrue();
        ClaimAutoAccept.GetBlockers(claimInfo).ShouldBeEmpty();
    }

    [Fact]
    public void DisabledAutoAcceptDoesNotApply()
    {
        var claimInfo = MakeClaimInfo(autoAcceptEnabled: false);

        ClaimAutoAccept.AppliesTo(claimInfo).ShouldBeFalse();
        ClaimAutoAccept.GetBlockers(claimInfo).ShouldBeEmpty();
    }

    /// <summary>
    /// Вне AppliesTo блокеры не спрашивают вовсе, даже если условия для них есть: исчерпанный
    /// лимит и невыданные данные ничего не «держат», когда автоприём заявке не грозит.
    /// </summary>
    [Theory]
    [InlineData(false, ClaimStatus.AddedByUser)]
    [InlineData(true, ClaimStatus.Approved)]
    public void BlockersAreNotCollectedOutsideAppliesTo(bool autoAcceptEnabled, ClaimStatus status)
    {
        var claimInfo = MakeClaimInfo(
            autoAcceptEnabled,
            claimStatus: status,
            slotLimit: 0,
            sensitiveDataRequired: true,
            playerAllowedSensitiveData: false);

        ClaimAutoAccept.GetBlockers(claimInfo).ShouldBeEmpty();
    }

    /// <summary>Автоприём — только для заявок на шаблон (#5645).</summary>
    [Fact]
    public void RegularCharacterDoesNotApply()
    {
        var claimInfo = MakeClaimInfo(autoAcceptEnabled: true, characterType: CharacterType.Player);

        ClaimAutoAccept.AppliesTo(claimInfo).ShouldBeFalse();
    }

    /// <summary>
    /// Принятой, отклонённой и прочим заявкам вне обсуждения автоприём не нужен: принимать нечего.
    /// </summary>
    [Theory]
    [InlineData(ClaimStatus.AddedByMaster)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.DeclinedByMaster)]
    [InlineData(ClaimStatus.DeclinedByUser)]
    [InlineData(ClaimStatus.OnHold)]
    [InlineData(ClaimStatus.CheckedIn)]
    public void ClaimOutsideDiscussionDoesNotApply(ClaimStatus status)
        => ClaimAutoAccept.AppliesTo(MakeClaimInfo(autoAcceptEnabled: true, claimStatus: status)).ShouldBeFalse();

    [Fact]
    public void ExhaustedSlotIsABlocker()
        => ClaimAutoAccept.GetBlockers(MakeClaimInfo(autoAcceptEnabled: true, slotLimit: 0))
            .ShouldBe([AutoAcceptBlocker.SlotExhausted]);

    /// <summary>null — лимита нет, места есть всегда.</summary>
    [Fact]
    public void UnlimitedSlotIsNotABlocker()
        => ClaimAutoAccept.GetBlockers(MakeClaimInfo(autoAcceptEnabled: true, slotLimit: null))
            .ShouldBeEmpty();

    [Fact]
    public void NotGrantedSensitiveDataIsABlockerWhenRequired()
        => ClaimAutoAccept.GetBlockers(MakeClaimInfo(
                autoAcceptEnabled: true,
                sensitiveDataRequired: true,
                playerAllowedSensitiveData: false))
            .ShouldBe([AutoAcceptBlocker.SensitiveDataNotGranted]);

    [Fact]
    public void GrantedSensitiveDataIsNotABlocker()
        => ClaimAutoAccept.GetBlockers(MakeClaimInfo(
                autoAcceptEnabled: true,
                sensitiveDataRequired: true,
                playerAllowedSensitiveData: true))
            .ShouldBeEmpty();

    /// <summary>Не запрошенные проектом данные игрок не обязан открывать.</summary>
    [Fact]
    public void NotRequestedSensitiveDataIsNotABlocker()
        => ClaimAutoAccept.GetBlockers(MakeClaimInfo(
                autoAcceptEnabled: true,
                sensitiveDataRequired: false,
                playerAllowedSensitiveData: false))
            .ShouldBeEmpty();

    [Fact]
    public void BothBlockersAreReportedTogether()
        => ClaimAutoAccept.GetBlockers(MakeClaimInfo(
                autoAcceptEnabled: true,
                slotLimit: 0,
                sensitiveDataRequired: true,
                playerAllowedSensitiveData: false))
            .ShouldBe([AutoAcceptBlocker.SlotExhausted, AutoAcceptBlocker.SensitiveDataNotGranted]);
}
