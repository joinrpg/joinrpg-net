using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.DomainTypes.Test.Characters;

public class CharacterTypeInfoTest
{
    private static CharacterTypeInfo Create(bool isPublic, bool hidePlayerForCharacter)
        => CharacterTypeInfo.Create(
            CharacterType.Player,
            isHot: false,
            characterSlotLimit: null,
            characterName: "Вася",
            isPublic,
            hidePlayerForCharacter);

    [Fact]
    public void PublicCharacterHasPublicName()
    {
        var typeInfo = Create(isPublic: true, hidePlayerForCharacter: false);

        typeInfo.CharacterVisibility.ShouldBe(CharacterVisibility.Public);
        typeInfo.IsPublic.ShouldBeTrue();
        typeInfo.IsNamePublic.ShouldBeTrue();
    }

    /// <summary>
    /// Главный случай, ради которого свойство и заведено: персонаж публичен, скрыт только игрок.
    /// По <see cref="CharacterTypeInfo.IsPublic"/> он выглядит непубличным, и публичные списки
    /// ролей вычёркивали бы такие роли целиком.
    /// </summary>
    [Fact]
    public void CharacterWithHiddenPlayerStillHasPublicName()
    {
        var typeInfo = Create(isPublic: true, hidePlayerForCharacter: true);

        typeInfo.CharacterVisibility.ShouldBe(CharacterVisibility.PlayerHidden);
        typeInfo.IsPublic.ShouldBeFalse();
        typeInfo.IsNamePublic.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrivateCharacterHasNoPublicName(bool hidePlayerForCharacter)
    {
        var typeInfo = Create(isPublic: false, hidePlayerForCharacter);

        typeInfo.CharacterVisibility.ShouldBe(CharacterVisibility.Private);
        typeInfo.IsPublic.ShouldBeFalse();
        typeInfo.IsNamePublic.ShouldBeFalse();
    }
}
