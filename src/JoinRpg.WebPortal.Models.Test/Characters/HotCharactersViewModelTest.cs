using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.WebPortal.Models.Test.Characters;

/// <summary>
/// Горячие роли для публичного JSON (<c>hotjson</c>) — плоский список, без дерева групп.
/// </summary>
public class HotCharactersViewModelTest
{
    private MockedProject Mock { get; } = new MockedProject();

    /// <summary>Посторонний: не мастер проекта и не игрок ни одной заявки.</summary>
    private static UserIdentification StrangerId => new(12345);

    private IReadOnlyCollection<CharacterViewModel> GetHotCharacters()
        => [.. HotCharactersViewModel.GetHotCharacters(
            [.. Mock.Project.Characters.Select(Mock.GetCharacterInfo)],
            StrangerId)];

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
        var hot = CreateHotCharacter();

        GetHotCharacters().Select(character => character.CharacterId).ShouldBe([hot.CharacterId]);
    }

    [Fact]
    public void NonPublicHotCharacterIsNotListed()
    {
        var hot = CreateHotCharacter();
        hot.IsPublic = false;

        GetHotCharacters().ShouldBeEmpty();
    }

    /// <summary>
    /// Публичный персонаж со скрытым игроком остаётся в списке — прячется только игрок.
    /// </summary>
    [Fact]
    public void HotCharacterWithHiddenPlayerIsListed()
    {
        var hot = CreateHotCharacter();
        hot.HidePlayerForCharacter = true;

        GetHotCharacters().Select(character => character.CharacterId).ShouldBe([hot.CharacterId]);
    }

    [Fact]
    public void DeletedHotCharacterIsNotListed()
    {
        var hot = CreateHotCharacter();
        hot.IsActive = false;

        GetHotCharacters().ShouldBeEmpty();
    }

    [Fact]
    public void NotHotCharacterIsNotListed()
    {
        Mock.Character.IsPublic = true;

        GetHotCharacters().ShouldBeEmpty();
    }
}
