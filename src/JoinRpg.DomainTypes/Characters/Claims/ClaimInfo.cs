using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Заявка вместе со своим персонажем и профилем игрока (ADR021). По ней, например, считаются
/// проблемы заявки и правила регистрации.
/// </summary>
/// <remarks>
/// <para>
/// Пара «персонаж + заявка» лежит в <see cref="ClaimInCharacter"/>, а здесь к ней добавляется
/// профиль. Тип хранит пару, а не наследует её, поэтому ту же пару можно передать дальше без
/// профиля, а равенство record не смешивает два типа.
/// </para>
/// <para>
/// Конструктор проверяет, что профиль принадлежит игроку заявки. Иначе потребителю можно было бы
/// передать заявку одного игрока вместе с профилем другого, и он молча посчитал бы чепуху.
/// </para>
/// </remarks>
public record class ClaimInfo
{
    /// <summary>Заявка в составе своего персонажа.</summary>
    public ClaimInCharacter ClaimInCharacter { get; }

    /// <summary>
    /// Профиль игрока, подавшего заявку.
    /// </summary>
    /// <remarks>
    /// Не nullable осознанно: у заявки всегда есть игрок — это инвариант, а не осторожность.
    /// Практическое следствие: вызывающий обязан загрузить профиль (для списков — пачкой, через
    /// <c>IUserRepository.GetRequiredUserInfos</c>), и проблемы «в профиле не хватает контактов»
    /// не могут молча исчезнуть оттого, что его поленились загрузить. Кому профиль не нужен,
    /// берут <see cref="ClaimInCharacter"/>.
    /// </remarks>
    public UserInfo Player { get; }

    /// <summary>Персонаж, на которого подана заявка.</summary>
    public CharacterInfo Character => ClaimInCharacter.Character;

    /// <summary>Заявка. Всегда одна из <see cref="CharacterInfo.Claims"/>.</summary>
    public CharacterClaimInfo Claim => ClaimInCharacter.Claim;

    /// <summary>Метаданные проекта — из агрегата персонажа.</summary>
    public ProjectInfo ProjectInfo => Character.ProjectInfo;

    public ClaimIdentification ClaimId => ClaimInCharacter.ClaimId;

    /// <summary>
    /// Защищённые данные игрока, которые мастер видит по этой заявке: их открывает согласие игрока
    /// на заявке, и только те, что проект запросил. Права текущего пользователя на заявку здесь не
    /// проверяются — это забота вызывающего.
    /// </summary>
    public ClaimSensitiveData SensitiveData
    {
        get
        {
            var requirements = ProjectInfo.ProfileRequirementSettings;
            var access = Claim.PlayerAllowedSensitiveData && requirements.SensitiveDataRequired;
            var showPassport = access && requirements.PassportRequested;
            var showAddress = access && requirements.RegistrationAddressRequested;
            var showBirthDate = access && requirements.BirthDateRequested;
            return new ClaimSensitiveData(
                showPassport,
                showPassport ? Player.PassportData : null,
                showAddress,
                showAddress ? Player.RegistrationAddress : null,
                showBirthDate,
                showBirthDate ? Player.BirthDate : null);
        }
    }

    public ClaimInfo(ClaimInCharacter claimInCharacter, UserInfo player)
    {
        ArgumentNullException.ThrowIfNull(claimInCharacter);
        ArgumentNullException.ThrowIfNull(player);

        if (player.UserId != claimInCharacter.Claim.PlayerId)
        {
            throw new ArgumentException(
                $"Profile of user {player.UserId} is passed for claim {claimInCharacter.ClaimId} of player {claimInCharacter.Claim.PlayerId}",
                nameof(player));
        }

        ClaimInCharacter = claimInCharacter;
        Player = player;
    }
}

/// <summary>
/// Защищённые данные игрока по заявке (см. <see cref="ClaimInfo.SensitiveData"/>). Флаг Show* —
/// «показывать поле мастеру»: значение может быть null и при открытом поле, если игрок его не заполнил.
/// </summary>
public record ClaimSensitiveData(
    bool ShowPassportData,
    string? PassportData,
    bool ShowRegistrationAddress,
    string? RegistrationAddress,
    bool ShowBirthDate,
    DateOnly? BirthDate);
