using Microsoft.EntityFrameworkCore;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl;

internal class AntiSpamServiceImpl : DbServiceImplBase, IAntiSpamService
{
    /// <inheritdoc />
    public async Task<bool> IsLarper(IsLarperRequest request)
    {
        return await UnitOfWork.GetDbSet<User>()
            .Where(user =>
                user.Email == request.Email ||
                user.Extra.Telegram == request.TelegramNickName ||
                user.Extra.Vk == request.VkNickName ||
                user.ExternalLogins.Any(el => el.Key == request.VkId && el.Provider == "Vkontakte")
            )
            .Where(user => user.Claims.Any(claim =>
                claim.ClaimStatus == Claim.Status.Approved ||
                claim.ClaimStatus == Claim.Status.CheckedIn))
            .AnyAsync();
    }

    /// <inheritdoc />
    public AntiSpamServiceImpl(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor) : base(unitOfWork, currentUserAccessor)
    {
    }
}
