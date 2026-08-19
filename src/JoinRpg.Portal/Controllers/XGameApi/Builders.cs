using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;
using JoinRpg.XGameApi.Contract;
// Доменный агрегат (ADR013) и DTO внешнего API называются одинаково — разводим псевдонимом.
using DomainCharacterInfo = JoinRpg.DomainTypes.Characters.CharacterInfo;

namespace JoinRpg.Portal.Controllers.XGameApi;

public class ApiInfoBuilder
{
    public static GroupHeader[] ToGroupHeaders(
    IReadOnlyCollection<Data.Interfaces.GroupHeader> characterDirectGroups)
    {
        return [.. characterDirectGroups.Where(group => group.IsActive && !group.IsSpecial)
            .Select(
                group => new GroupHeader
                {
                    CharacterGroupId = group.CharacterGroupId,
                    CharacterGroupName = group.CharacterGroupName,
                })
            .OrderBy(group => group.CharacterGroupId)];
    }

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

    public static CharacterPlayerInfo? CreatePlayerInfo(Claim? claim, ProjectInfo projectInfo)
    {
        if (claim is null)
        {
            return null;
        }
        return new CharacterPlayerInfo(
                                    claim.PlayerUserId,
                                    claim.ClaimFeeDue(projectInfo) <= 0,
                                    claim.Player.ExtractDisplayName().DisplayName,
                                    ToPlayerContacts(claim.Player)
                                    );
    }

    /// <summary>
    /// Сведения об игроке поверх доменного агрегата (ADR013). Отображаемые данные игрока в агрегат
    /// не входят — они приходят отдельно из <c>IUserRepository</c>.
    /// </summary>
    public static CharacterPlayerInfo CreatePlayerInfo(
        DomainCharacterInfo character,
        CharacterClaimInfo claim,
        UserInfo player)
        => new(
            claim.PlayerId.Value,
            character.CalculateClaimBalance(claim, character.ProjectInfo).FeeDue <= 0,
            player.DisplayName.DisplayName,
            ToPlayerContacts(player));

    public static PlayerContacts ToPlayerContacts(User player)
    {
        return new PlayerContacts(player.Email, player.Extra?.PhoneNumber,
                                                player.Extra?.VkVerified == true ? player.Extra?.Vk : null,
                                                player.Extra?.Telegram);
    }

    public static PlayerContacts ToPlayerContacts(UserInfo player)
    {
        return new PlayerContacts(player.Email, player.PhoneNumber,
                                                // Контракт PlayerContacts обещает отдавать только
                                                // подтверждённый VK — как и версия для User.
                                                player.Social.Vk is { IsVerified: true } vk ? $"id{vk.Id}" : null,
                                                player.Social.Telegram?.PrettyName?.Value);
    }

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
