using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Раскладка <see cref="CharacterTypeInfo"/> по колонкам персонажа. Разбор случаев имени слота —
/// в тестах <see cref="CharacterTypeInfo.GetSlotCharacterName"/>, здесь только что оно доходит до сущности.
/// </summary>
public class ApplyTypeSettingsTest
{
    private readonly MockedProject mock = new();

    [Fact]
    public void WritesTypeFlagsAndActivates()
    {
        // Был слотом с лимитом и неактивным: настройки нового типа должны перезаписать оба поля.
        var character = mock.CreateSlot("Вася", slotLimit: 5);
        character.IsActive = false;

        character.ApplyTypeSettings(
            new CharacterTypeInfo(CharacterType.Player, IsHot: true, SlotLimit: null, SlotName: null, CharacterVisibility.PlayerHidden),
            mock.ProjectInfo);

        character.CharacterType.ShouldBe(CharacterType.Player);
        character.IsHot.ShouldBeTrue();
        character.CharacterSlotLimit.ShouldBeNull();
        character.IsAcceptingClaims.ShouldBeTrue();
        character.IsPublic.ShouldBeTrue();
        character.HidePlayerForCharacter.ShouldBeTrue();
        character.IsActive.ShouldBeTrue();
        character.CharacterName.ShouldBe("Вася");
    }

    [Fact]
    public void SlotGetsSlotNameAndLimit()
    {
        var character = mock.CreateCharacter("Вася");

        character.ApplyTypeSettings(
            new CharacterTypeInfo(CharacterType.Slot, IsHot: false, SlotLimit: 3, SlotName: " Стражник ", CharacterVisibility.Public),
            mock.ProjectInfo);

        character.CharacterType.ShouldBe(CharacterType.Slot);
        character.CharacterSlotLimit.ShouldBe(3);
        character.CharacterName.ShouldBe("Стражник");
    }
}
