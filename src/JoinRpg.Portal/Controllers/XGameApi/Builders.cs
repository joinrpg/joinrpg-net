using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.Users;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.Portal.Controllers.XGameApi;

public class ApiInfoBuilder
{
    public static IOrderedEnumerable<GroupHeader> ToGroupHeaders(
    IReadOnlyCollection<Data.Interfaces.GroupHeader> characterDirectGroups)
    {
        return characterDirectGroups.Where(group => group.IsActive && !group.IsSpecial)
            .Select(
                group => new GroupHeader
                {
                    CharacterGroupId = group.CharacterGroupId,
                    CharacterGroupName = group.CharacterGroupName,
                })
            .OrderBy(group => group.CharacterGroupId);
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

    public static PlayerContacts ToPlayerContacts(User player)
    {
        return new PlayerContacts(player.Email, player.Extra?.PhoneNumber,
                                                player.Extra?.VkVerified == true ? player.Extra?.Vk : null,
                                                player.Extra?.Telegram);
    }

    public static PlayerContacts ToPlayerContacts(UserInfo player)
    {
        return new PlayerContacts(player.Email, player.PhoneNumber,
                                                player.Social.VkId,
                                                player.Social.TelegramId?.UserName?.Value);
    }
}
