namespace JoinRpg.DomainTypes.Characters;

public record CharacterTypeInfo
{
    public CharacterType CharacterType { get; }

    public bool IsHot { get; }

    public bool IsAcceptingClaims => CharacterType != CharacterType.NonPlayer;

    public bool IsPublic => CharacterVisibility == CharacterVisibility.Public;

    public int? SlotLimit { get; }

    public string? SlotName { get; }

    public CharacterVisibility CharacterVisibility { get; }

    public CharacterTypeInfo(
        CharacterType CharacterType,
        bool IsHot,
        int? SlotLimit,
        string? SlotName,
        CharacterVisibility CharacterVisibility)
    {
        this.CharacterType = CharacterType;
        if (IsHot && CharacterType == CharacterType.NonPlayer)
        {
            throw new ArgumentException("NPCs are not hot", nameof(IsHot));
        }

        if (SlotLimit is not null && CharacterType != CharacterType.Slot)
        {
            throw new ArgumentException("Not-slots could not be limited", nameof(SlotLimit));
        }

        if (SlotName is not null && CharacterType != CharacterType.Slot)
        {
            throw new ArgumentException("Not-slots could not have slot names", nameof(SlotLimit));
        }

        this.IsHot = IsHot;
        this.SlotLimit = SlotLimit;
        this.SlotName = SlotName;
        this.CharacterVisibility = CharacterVisibility;
    }

    /// <summary>
    /// Собирает <see cref="CharacterTypeInfo"/> из «сырых» флагов персонажа, как они хранятся
    /// в БД. Единственное место, где живёт этот маппинг — иначе он расходится между
    /// загрузчиками (см. ADR013).
    /// </summary>
    public static CharacterTypeInfo Create(
        CharacterType characterType,
        bool isHot,
        int? characterSlotLimit,
        string characterName,
        bool isPublic,
        bool hidePlayerForCharacter)
        => new(
            characterType,
            isHot,
            characterSlotLimit,
            characterType == CharacterType.Slot ? characterName : null,
            GetVisibility(isPublic, hidePlayerForCharacter));

    /// <summary>
    /// Маппинг пары флагов в видимость. Асимметричен: при <paramref name="isPublic"/> == false
    /// значение <paramref name="hidePlayerForCharacter"/> роли не играет, поэтому обратно из
    /// <see cref="CharacterVisibility"/> исходную пару восстановить нельзя.
    /// </summary>
    public static CharacterVisibility GetVisibility(bool isPublic, bool hidePlayerForCharacter)
        => (isPublic, hidePlayerForCharacter) switch
        {
            (false, _) => CharacterVisibility.Private,
            (true, false) => CharacterVisibility.Public,
            (true, true) => CharacterVisibility.PlayerHidden,
        };

    public static CharacterTypeInfo Default() => new(CharacterType.Player, false, null, null, CharacterVisibility.Public);

    public static CharacterTypeInfo DefaultSlot(string slotName)
    {
        return new(
                                    CharacterType.Slot,
                                    IsHot: false,
                                    SlotLimit: null,
                                    SlotName: slotName,
                                    CharacterVisibility.Public);
    }

    public void Deconstruct(
        out CharacterType characterType,
        out bool isHot,
        out int? slotLimit,
        out bool isAcceptingClaims,
        out string? slotName,
        out bool isPublic,
        out bool isPlayerHidden)
    {
        characterType = CharacterType;
        isHot = IsHot;
        slotLimit = SlotLimit;
        isAcceptingClaims = CharacterType != CharacterType.NonPlayer;
        slotName = SlotName;
        (isPublic, isPlayerHidden) = CharacterVisibility switch
        {
            CharacterVisibility.Public => (true, false),
            CharacterVisibility.PlayerHidden => (true, true),
            CharacterVisibility.Private => (false, true),
            _ => throw new NotImplementedException(),
        };
    }

    public void Deconstruct(out CharacterType characterType, out bool isHot, out int? slotLimit)
    {
        characterType = CharacterType;
        isHot = IsHot;
        slotLimit = SlotLimit;
    }
}

public enum CharacterType
{
    Player,
    NonPlayer,
    Slot
}

public enum CharacterVisibility
{
    Public,
    PlayerHidden,
    Private,
}
