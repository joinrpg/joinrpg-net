using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.WebPortal.Models.Test.Characters;

/// <summary>
/// Горячие роли для публичного JSON (<c>hotjson</c>) — плоский список, без дерева групп.
/// </summary>
public class HotCharactersViewModelTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private CharacterGroup RootGroup => Mock.Project.CharacterGroups.Single(group => group.IsRoot);

    private CharacterGroupInfo Root => Mock.ProjectInfo.GetGroupById(RootGroup.CharacterGroupId);

    /// <summary>Посторонний: не мастер проекта и не игрок ни одной заявки.</summary>
    private static UserIdentification StrangerId => new(12345);

    private void MakeRootGroupPublic()
    {
        RootGroup.IsPublic = true;
        Mock.ReInitProjectInfo();
    }

    private IReadOnlyCollection<CharacterViewModel> GetHotCharacters()
        => [.. HotCharactersViewModel.GetHotCharacters(
            Root,
            [.. Mock.Project.Characters.Select(Mock.GetCharacterInfo)],
            StrangerId,
            Mock.ProjectInfo)];

    private Character CreateHotCharacter()
    {
        var hot = Mock.CreateCharacter("горячая");
        hot.IsPublic = true;
        hot.IsHot = true;
        return hot;
    }

    [Fact]
    public void HotCharacterIsListed()
    {
        MakeRootGroupPublic();
        Mock.Character.IsPublic = true;
        var hot = CreateHotCharacter();

        GetHotCharacters().Select(character => character.CharacterId).ShouldBe([hot.CharacterId]);
    }

    [Fact]
    public void HotCharacterOfInvisibleGroupIsNotListed()
    {
        // Корневая группа непубличная, значит постороннему не видно и всё, что в ней.
        _ = CreateHotCharacter();

        GetHotCharacters().ShouldBeEmpty();
    }

    [Fact]
    public void DeletedHotCharacterIsNotListed()
    {
        MakeRootGroupPublic();
        var hot = CreateHotCharacter();
        hot.IsActive = false;

        GetHotCharacters().ShouldBeEmpty();
    }
}
