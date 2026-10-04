using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Всё, по чему считаются проблемы одной заявки (ADR013): сам персонаж, заявка на него и профиль
/// игрока.
/// </summary>
/// <remarks>
/// <para>
/// Отдельный тип-контекст, а не три параметра у каждого фильтра: проблемы заявки смотрят то на
/// персонажа (занят ли он), то на проект (ответственный мастер, настройки взносов), то на профиль
/// игрока (контакты). Собрав это в один объект, мы заодно получаем место, где проверяется их
/// согласованность — иначе фильтру можно было бы передать заявку одного персонажа вместе с
/// профилем чужого игрока, и он молча посчитал бы чепуху.
/// </para>
/// <para>
/// <see cref="ProjectInfo"/> отдельным полем не хранится: он лежит внутри агрегата персонажа, и
/// второй канал тех же метаданных означал бы, что их можно передать несогласованными.
/// </para>
/// </remarks>
public record class ClaimProblemContext
{
    /// <summary>Персонаж, на которого подана заявка.</summary>
    public CharacterInfo Character { get; }

    /// <summary>Заявка, чьи проблемы считаются. Всегда одна из <see cref="CharacterInfo.Claims"/>.</summary>
    public CharacterClaimInfo Claim { get; }

    /// <summary>
    /// Профиль игрока, подавшего заявку.
    /// </summary>
    /// <remarks>
    /// Не nullable осознанно: у заявки всегда есть игрок — это инвариант, а не осторожность.
    /// Конструктор его и проверяет, сверяя <c>Player.UserId</c> с <c>Claim.PlayerId</c>.
    /// Практическое следствие: вызывающий обязан загрузить профиль (для списков — пачкой, через
    /// <c>IUserRepository.GetRequiredUserInfos</c>), и проблемы «в профиле не хватает контактов»
    /// не могут молча исчезнуть оттого, что его поленились загрузить.
    /// </remarks>
    public UserInfo Player { get; }

    /// <summary>Метаданные проекта — из агрегата персонажа.</summary>
    public ProjectInfo ProjectInfo => Character.ProjectInfo;

    public ClaimProblemContext(CharacterInfo character, CharacterClaimInfo claim, UserInfo player)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(player);

        if (!character.Claims.Any(c => c.ClaimId == claim.ClaimId))
        {
            throw new ArgumentException(
                $"Claim {claim.ClaimId} is not among claims of character {character.Id}", nameof(claim));
        }

        if (player.UserId != claim.PlayerId)
        {
            throw new ArgumentException(
                $"Profile of user {player.UserId} is passed for claim {claim.ClaimId} of player {claim.PlayerId}",
                nameof(player));
        }

        Character = character;
        Claim = claim;
        Player = player;
    }
}
