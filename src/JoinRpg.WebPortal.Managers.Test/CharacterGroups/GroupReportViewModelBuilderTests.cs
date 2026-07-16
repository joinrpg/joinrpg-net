using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.WebPortal.Managers.CharacterGroups;

namespace JoinRpg.WebPortal.Managers.Test.CharacterGroups;

public class GroupReportViewModelBuilderTests
{
    private readonly MockedProject _mock = new();

    private CharacterGroup RootGroup => _mock.Project.CharacterGroups.Single(g => g.IsRoot);

    private CharacterGroup MakeChildOfRoot()
    {
        _mock.Group.ParentCharacterGroupIds = [RootGroup.CharacterGroupId];
        return _mock.Group;
    }

    [Fact]
    public void Build_NoChildGroups_OnlyTotalRow()
    {
        var result = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: false);

        result.GroupId.ShouldBe(RootGroup.GetId());
        var row = result.Rows.ShouldHaveSingleItem();
        row.Group.CharacterGroupId.ShouldBe(result.GroupId);
    }

    [Fact]
    public void Build_ChildGroup_HasGroupLinkWithNameAndVisibility()
    {
        var child = MakeChildOfRoot();
        child.IsPublic = true;

        var result = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: false);

        result.Rows.Count.ShouldBe(2);
        var childRow = result.Rows.Single(r => r.Group.CharacterGroupId != result.GroupId);
        childRow.Group.CharacterGroupId.ShouldBe(child.GetId());
        childRow.Group.Name.ShouldBe(child.CharacterGroupName);
        childRow.Group.IsPublic.ShouldBeTrue();
    }

    [Fact]
    public void Build_PrivateChildGroup_GroupIsPublicFalse()
    {
        var child = MakeChildOfRoot();
        child.IsPublic = false;

        var result = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: false);

        result.Rows.Single(r => r.Group.CharacterGroupId != result.GroupId).Group.IsPublic.ShouldBeFalse();
    }

    [Fact]
    public void Build_InactiveChildGroup_ExcludedFromRows()
    {
        var child = MakeChildOfRoot();
        child.IsActive = false;

        var result = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: false);

        result.Rows.ShouldHaveSingleItem().Group.CharacterGroupId.ShouldBe(result.GroupId);
    }

    [Fact]
    public void Build_CharactersInChildGroup_CountedInBothChildAndTotalRow()
    {
        var child = MakeChildOfRoot();
        var totalBefore = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: false)
            .Rows.Single(r => r.Group.CharacterGroupId == RootGroup.GetId()).TotalCharacters;
        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [child.CharacterGroupId];

        var result = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: false);

        result.Rows.Single(r => r.Group.CharacterGroupId == result.GroupId).TotalCharacters.ShouldBe(totalBefore + 1);
        result.Rows.Single(r => r.Group.CharacterGroupId != result.GroupId).TotalCharacters.ShouldBe(1);
    }

    [Fact]
    public void Build_ApprovedClaim_CountsAsCharacterWithPlayerAndAcceptedClaim()
    {
        var child = MakeChildOfRoot();
        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [child.CharacterGroupId];
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: false);
        var row = result.Rows.Single(r => r.Group.CharacterGroupId != result.GroupId);

        row.TotalCharactersWithPlayers.ShouldBe(1);
        row.TotalAcceptedClaims.ShouldBe(1);
        row.TotalFreeSlots.ShouldBe(0);
    }

    [Fact]
    public void Build_NpcCharacter_CountedAsNpcWithNoFreeSlot()
    {
        var child = MakeChildOfRoot();
        var character = _mock.CreateCharacter("Страж");
        character.ParentCharacterGroupIds = [child.CharacterGroupId];
        character.CharacterType = CharacterType.NonPlayer;

        var result = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: false);
        var row = result.Rows.Single(r => r.Group.CharacterGroupId != result.GroupId);

        row.TotalNpcCharacters.ShouldBe(1);
        row.TotalFreeSlots.ShouldBe(0);
        row.TotalSlots.ShouldBe(1);
    }

    [Fact]
    public void Build_SlotCharacterWithoutLimit_MarkedUnlimited()
    {
        var child = MakeChildOfRoot();
        var character = _mock.CreateCharacter("Шаблон");
        character.ParentCharacterGroupIds = [child.CharacterGroupId];
        character.CharacterType = CharacterType.Slot;
        character.CharacterSlotLimit = null;

        var result = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: false);
        var row = result.Rows.Single(r => r.Group.CharacterGroupId != result.GroupId);

        row.Unlimited.ShouldBeTrue();
        row.TotalSlots.ShouldBe(1);
    }

    [Fact]
    public void Build_CheckinModuleEnabled_FlowsToViewModel()
    {
        var result = GroupReportViewModelBuilder.Build(RootGroup, checkinModuleEnabled: true);

        result.CheckinModuleEnabled.ShouldBeTrue();
    }
}
