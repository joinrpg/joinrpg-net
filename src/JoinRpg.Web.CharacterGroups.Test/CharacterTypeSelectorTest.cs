using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Web.CharacterGroups.Test;

/// <summary>
/// #5678: в проекте без поля имени персонаж без активных заявок открывается шаблоном.
/// Название шаблона должно браться из имени персонажа, а не оставаться пустым.
/// </summary>
public class CharacterTypeSelectorTest
{
    private const string CharacterName = "Участник конвента";

    private static (string CharacterType, string? SlotName) Render(CharacterTypeInfo initialModel)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<CharacterTypeSelector>(p => p
            .Add(x => x.InitialModel, initialModel)
            .Add(x => x.ActiveClaimsCount, 0)
            .Add(x => x.CharactersHaveNameField, false)
            .Add(x => x.CharacterName, CharacterName));

        return (
            cut.Find("input[name='CharacterTypeInfo.CharacterType']").GetAttribute("value")!,
            cut.Find("input[name='CharacterTypeInfo.SlotName']").GetAttribute("value"));
    }

    [Fact]
    public void PlayerWithoutActiveClaims_BecomesSlotNamedAfterCharacter()
        => Render(new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public))
            .ShouldBe(("Slot", CharacterName));

    [Fact]
    public void Slot_KeepsItsOwnSlotName()
        => Render(CharacterTypeInfo.DefaultSlot("Стражник"))
            .ShouldBe(("Slot", "Стражник"));
}
