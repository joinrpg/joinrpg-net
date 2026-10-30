using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

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
            hidePlayerForCharacter,
            isAcceptingClaims: true);

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
    [InlineData(CharacterType.Player)]
    [InlineData(CharacterType.Slot)]
    public void ClaimsCanBeClosedForPlayers(CharacterType characterType)
        => new CharacterTypeInfo(characterType, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public, IsAcceptingClaims: false)
            .IsAcceptingClaims.ShouldBeFalse();

    [Fact]
    public void NpcNeverAcceptsClaims()
        => new CharacterTypeInfo(CharacterType.NonPlayer, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public, IsAcceptingClaims: true)
            .IsAcceptingClaims.ShouldBeFalse();

    // Горячую роль рекламируют с кнопкой «Подать заявку».
    [Fact]
    public void HotRoleMustAcceptClaims()
        => Should.Throw<ArgumentException>(
            () => new CharacterTypeInfo(CharacterType.Player, IsHot: true, SlotLimit: null, SlotName: null, CharacterVisibility.Public, IsAcceptingClaims: false));

    [Fact]
    public void DeconstructReturnsStoredFlag()
    {
        var (_, _, _, isAcceptingClaims, _, _, _) =
            new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public, IsAcceptingClaims: false);

        isAcceptingClaims.ShouldBeFalse();
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

    private static readonly ProjectInfo ProjectWithoutNameField = ProjectInfoFixture.Build();

    private static CharacterTypeInfo Slot(string? slotName)
        => new(CharacterType.Slot, IsHot: false, SlotLimit: null, slotName, CharacterVisibility.Public);

    [Fact]
    public void SlotWithoutNameFieldGetsTrimmedSlotName()
        => Slot("  Стражник ").GetSlotCharacterName(ProjectWithoutNameField).ShouldBe("Стражник");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SlotWithoutNameFieldRequiresSlotName(string? slotName)
        => Should.Throw<FieldRequiredException>(() => Slot(slotName).GetSlotCharacterName(ProjectWithoutNameField))
            .FieldName.ShouldBe("characterTypeInfo.SlotName");

    /// <summary>
    /// С полем-именем имя персонажа пишется через поле, а не из настроек типа — даже пустое имя
    /// слота тогда не ошибка.
    /// </summary>
    [Fact]
    public void SlotWithNameFieldDoesNotSetName()
    {
        var nameField = ProjectInfoFixture.MakeField(1);
        var project = ProjectInfoFixture.Build(fields: [nameField], nameField: nameField.Id);

        Slot(slotName: null).GetSlotCharacterName(project).ShouldBeNull();
    }

    [Theory]
    [InlineData(CharacterType.Player)]
    [InlineData(CharacterType.NonPlayer)]
    public void NotSlotDoesNotSetName(CharacterType characterType)
        => new CharacterTypeInfo(characterType, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public)
            .GetSlotCharacterName(ProjectWithoutNameField).ShouldBeNull();
}
