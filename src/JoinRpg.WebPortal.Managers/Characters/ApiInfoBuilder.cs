using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Users;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.WebPortal.Managers.Characters;

public class ApiInfoBuilder
{
    public static GroupHeader[] ToGroupHeaders(
    IReadOnlyCollection<CharacterGroupInfo> characterDirectGroups)
    {
        return [.. characterDirectGroups.Where(group => group.IsActive && !group.IsSpecial)
            .Select(
                group => new GroupHeader
                {
                    CharacterGroupId = group.Id.CharacterGroupId,
                    CharacterGroupName = group.Name,
                })
            .OrderBy(group => group.CharacterGroupId)];
    }

    /// <summary>
    /// Сведения об игроке поверх утверждённой заявки (ADR021): профиль игрока приходит внутри
    /// <see cref="ClaimInfo"/>, загруженный пачкой через <c>IClaimInfoRepository</c>.
    /// </summary>
    public static CharacterPlayerInfo CreatePlayerInfo(ClaimInfo claim)
        => new(
            claim.Claim.PlayerId.Value,
            claim.ClaimInCharacter.CalculateBalance().FeeDue <= 0,
            claim.Player.DisplayName.DisplayName,
            ToPlayerContacts(claim.Player));

    public static PlayerContacts ToPlayerContacts(UserInfo player)
    {
        return new PlayerContacts(player.Email, player.PhoneNumber,
                                                // Контракт PlayerContacts обещает отдавать только
                                                // подтверждённый VK — как и версия для User.
                                                player.Social.Vk is { IsVerified: true } vk ? $"id{vk.Id}" : null,
                                                player.Social.Telegram?.PrettyName?.Value);
    }

    /// <summary>
    /// Статус заявки во внешнем контракте. Отображение явное, а не приведением по числу:
    /// доменный enum и enum контракта живут отдельно, и перестановка в одном не должна молча
    /// менять смысл в другом.
    /// </summary>
    public static ClaimStatusEnum ToApiStatus(ClaimStatus status) => status switch
    {
        ClaimStatus.AddedByUser => ClaimStatusEnum.AddedByUser,
        ClaimStatus.AddedByMaster => ClaimStatusEnum.AddedByMaster,
        ClaimStatus.Approved => ClaimStatusEnum.Approved,
        ClaimStatus.DeclinedByUser => ClaimStatusEnum.DeclinedByUser,
        ClaimStatus.DeclinedByMaster => ClaimStatusEnum.DeclinedByMaster,
        ClaimStatus.Discussed => ClaimStatusEnum.Discussed,
        ClaimStatus.OnHold => ClaimStatusEnum.OnHold,
        ClaimStatus.CheckedIn => ClaimStatusEnum.CheckedIn,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static FieldValue ToFieldValue(FieldWithValue field)
    {
        return new FieldValue
        {
            ProjectFieldId = field.Field.Id.ProjectFieldId,
            Value = field.Value,
            DisplayString = field.DisplayString,
        };
    }
}
