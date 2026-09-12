using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Data.Interfaces.Characters;

/// <summary>
/// Персонаж для списков выбора: то, что нужно показать, плюс то, что нужно правилам заявки.
/// </summary>
/// <remarks>
/// Намеренно не <see cref="CharacterInfo"/>: тому на каждого персонажа нужны все заявки с
/// финансами, датами комментариев и значениями полей, а списку хватает имени, описания и ответа
/// правил. Реализует <see cref="IClaimTarget"/> — узкий интерфейс как раз для этого и заводился,
/// так что правила применяются к проекции напрямую, без доменного агрегата и без адаптера над
/// EF-сущностью.
/// </remarks>
public record class CharacterListEntry(
    CharacterIdentification Id,
    string CharacterName,
    string Description,
    bool IsPublic,
    bool IsActive,
    CharacterTypeInfo CharacterTypeInfo,
    ClaimIdentification? ApprovedClaimId,
    IReadOnlyCollection<UserIdentification> ActiveClaimPlayerIds) : IClaimTarget
{
    public bool HasActiveClaimOf(UserIdentification userId) => ActiveClaimPlayerIds.Contains(userId);
}
