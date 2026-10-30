using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters;

public record CharacterTypeInfo
{
    public CharacterType CharacterType { get; }

    public bool IsHot { get; }

    /// <summary>
    /// Игрок может сам подать заявку на этого персонажа. Если нет — попасть на него можно только
    /// через мастера: приглашением или переносом заявки. У NPC всегда <c>false</c>.
    /// </summary>
    public bool IsAcceptingClaims { get; }

    /// <summary>Шаблон персонажа (слот): из него создаются персонажи по заявкам, сам он не играется и не печатается.</summary>
    public bool IsSlot => CharacterType == CharacterType.Slot;

    public bool IsPublic => CharacterVisibility == CharacterVisibility.Public;

    /// <summary>
    /// Имя персонажа видно всем — то есть выставлена колонка <c>Character.IsPublic</c>.
    /// </summary>
    /// <remarks>
    /// Отличается от <see cref="IsPublic"/>: у публичного персонажа со скрытым игроком видимость
    /// равна <see cref="CharacterVisibility.PlayerHidden"/>, и сам он при этом публичен —
    /// не показывается только игрок. Публичные списки ролей фильтруются именно по этому признаку.
    /// </remarks>
    public bool IsNamePublic => CharacterVisibility != CharacterVisibility.Private;

    public int? SlotLimit { get; }

    public string? SlotName { get; }

    public CharacterVisibility CharacterVisibility { get; }

    public CharacterTypeInfo(
        CharacterType CharacterType,
        bool IsHot,
        int? SlotLimit,
        string? SlotName,
        CharacterVisibility CharacterVisibility,
        // Дефолт true держит MVC-биндинг формы персонажа: не запостила поле — заявки открыты, а не
        // молча закрыты (и горячая роль не падает на инварианте ниже).
        bool IsAcceptingClaims = true)
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

        // NPC заявок не принимает по определению, флаг для него не имеет смысла.
        IsAcceptingClaims = IsAcceptingClaims && CharacterType != CharacterType.NonPlayer;

        // Горячую роль рекламируют с кнопкой «Подать заявку» — на закрытой для игроков роли она бы врала.
        if (IsHot && !IsAcceptingClaims)
        {
            throw new ArgumentException("Hot role should accept claims from players", nameof(IsHot));
        }

        this.IsAcceptingClaims = IsAcceptingClaims;
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
        bool hidePlayerForCharacter,
        bool isAcceptingClaims)
        => new(
            characterType,
            isHot,
            characterSlotLimit,
            characterType == CharacterType.Slot ? characterName : null,
            GetVisibility(isPublic, hidePlayerForCharacter),
            isAcceptingClaims);

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

    /// <summary>
    /// Обратный к <see cref="GetVisibility"/> маппинг: пара флагов, которую пишет доменное создание.
    /// У скрытого персонажа игрок тоже скрыт.
    /// </summary>
    public static (bool IsPublic, bool HidePlayerForCharacter) GetVisibilityFlags(CharacterVisibility visibility)
        => visibility switch
        {
            CharacterVisibility.Public => (true, false),
            CharacterVisibility.PlayerHidden => (true, true),
            CharacterVisibility.Private => (false, true),
            _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, "Unknown character visibility"),
        };

    /// <summary>
    /// Имя, которое персонаж получает из настроек типа: у слота это <see cref="SlotName"/>, но только
    /// пока в проекте нет поля-имени — иначе имя пишется через поле.
    /// </summary>
    /// <returns>Обрезанное имя слота; <c>null</c>, если тип имя персонажа не задаёт.</returns>
    /// <exception cref="FieldRequiredException">Имя слота нужно, но не заполнено.</exception>
    public string? GetSlotCharacterName(ProjectInfo projectInfo)
    {
        if (CharacterType != CharacterType.Slot || projectInfo.CharacterNameField is not null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(SlotName))
        {
            // Имя поля — как его раньше подставлял CallerArgumentExpression в ServiceValidation.Required.
            throw new FieldRequiredException("characterTypeInfo.SlotName");
        }

        return SlotName.Trim();
    }

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
        isAcceptingClaims = IsAcceptingClaims;
        slotName = SlotName;
        (isPublic, isPlayerHidden) = GetVisibilityFlags(CharacterVisibility);
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
