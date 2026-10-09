using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Заявка в составе своего агрегата (ADR021): персонаж и одна из его заявок.
/// </summary>
/// <remarks>
/// <para>
/// Отдельный тип, а не пара параметров: корень агрегата — персонаж (ADR014), и
/// <see cref="CharacterClaimInfo"/> сам по себе персонажа не знает. Передавая их по отдельности,
/// можно незаметно подсунуть заявку чужого персонажа. Здесь их согласованность проверяется
/// конструктором.
/// </para>
/// <para>
/// <see cref="ProjectInfo"/> отдельным полем не хранится: он лежит внутри агрегата персонажа, и
/// второй канал тех же метаданных означал бы, что их можно передать несогласованными.
/// </para>
/// </remarks>
public record class ClaimInCharacter
{
    /// <summary>Персонаж, на которого подана заявка.</summary>
    public CharacterInfo Character { get; }

    /// <summary>Заявка. Всегда одна из <see cref="CharacterInfo.Claims"/>.</summary>
    public CharacterClaimInfo Claim { get; }

    /// <summary>Метаданные проекта — из агрегата персонажа.</summary>
    public ProjectInfo ProjectInfo => Character.ProjectInfo;

    public ClaimIdentification ClaimId => Claim.ClaimId;

    /// <summary>
    /// Поля глазами самой заявки, без фильтрации по правам зрителя — для расчётов (взносы).
    /// </summary>
    /// <remarks>
    /// Утверждённая заявка видит персонажа целиком. Неутверждённая — только его публичные поля
    /// (<see cref="CharacterFieldLayers.ForUnapprovedClaim"/>): её игрок к персонажу доступа не
    /// имеет, и платить за скрытые от него поля персонажа не должен. Так считала и EF-версия
    /// (<c>Claim.GetFields</c>).
    /// </remarks>
    public IReadOnlyCollection<FieldWithValue> GetAllFields()
        => Claim.IsApproved
            ? Character.GetAllFields(ClaimId)
            // AccessArguments.None здесь безвреден: GetAllFieldsForEdit прав не смотрит.
            : CharacterFieldLayers.ForUnapprovedClaim(Claim.Fields, Character.CharacterFields, AccessArguments.None)
                .GetAllFieldsForEdit();

    /// <exception cref="KeyNotFoundException">У персонажа нет такой заявки.</exception>
    public ClaimInCharacter(CharacterInfo character, ClaimIdentification claimId)
        : this(character, (character ?? throw new ArgumentNullException(nameof(character))).GetClaimById(claimId))
    {
    }

    public ClaimInCharacter(CharacterInfo character, CharacterClaimInfo claim)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(claim);

        if (!character.Claims.Any(c => c.ClaimId == claim.ClaimId))
        {
            throw new ArgumentException(
                $"Claim {claim.ClaimId} is not among claims of character {character.Id}", nameof(claim));
        }

        Character = character;
        Claim = claim;
    }
}
