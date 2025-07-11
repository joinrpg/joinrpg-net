using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Avatars;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl;

/// <inheritdoc />
/// <summary>
/// ctor
/// </summary>
public class UserServiceImpl(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<UserServiceImpl> logger,
    IAvatarStorageService avatarStorageService
        ) : DbServiceImplBase(unitOfWork, currentUserAccessor), IUserService, IAvatarService
{

    /// <inheritdoc />
    public async Task UpdateProfile(int userId,
        UserFullName userFullName,
        Gender gender,
        string phoneNumber,
        string nicknames,
        string groupNames,
        string skype,
        string livejournal,
        ContactsAccessType socialAccessType)
    {
        if (CurrentUserId != userId)
        {
            throw new JoinRpgInvalidUserException();
        }
        var user = await UserRepository.WithProfile(userId);

        if (!user.VerifiedProfileFlag)
        {
            user.SurName = userFullName.SurName?.Value;
            user.FatherName = userFullName.FatherName?.Value;
            user.BornName = userFullName.BornName?.Value;
        }
        user.PrefferedName = userFullName.PrefferedName?.Value;

        user.Extra ??= new UserExtra();
        user.Extra.Gender = gender;

        if (!user.VerifiedProfileFlag)
        {
            user.Extra.PhoneNumber = phoneNumber;
        }
        user.Extra.Nicknames = nicknames;
        user.Extra.GroupNames = groupNames;
        user.Extra.Skype = skype;
        string[] tokensToRemove = ["http://", "https://", "vk.com", "vkontakte.ru", ".livejournal.com", ".lj.ru", "t.me", "/",];
        user.Extra.Livejournal = livejournal?.RemoveFromString(tokensToRemove);

        user.Extra.SocialNetworksAccess = socialAccessType;

        await UnitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task SetAdminFlag(int userId, bool administratorFlag)
    {
        if (!IsCurrentUserAdmin)
        {
            throw new MustBeAdminException();
        }
        var user = await UserRepository.GetById(userId);
        user.Auth.IsAdmin = administratorFlag;
        //TODO: Send email
        await UnitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task SetVerificationFlag(int userId, bool verificationFlag)
    {
        var user = await UserRepository.GetById(userId);
        if (!IsCurrentUserAdmin)
        {
            throw new MustBeAdminException();
        }
        user.VerifiedProfileFlag = verificationFlag;
        //TODO: Send email
        await UnitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task SetNameIfNotSetWithoutAccessChecks(int userId, UserFullName userFullName)
    {
        logger.LogInformation("Started to get names for {userId}. {userFullName}", userId, userFullName);
        var user = await UserRepository.WithProfile(userId);

        if (user.VerifiedProfileFlag)
        {
            logger.LogDebug("Skiping operating on verifying user");
            return;
        }

        user.PrefferedName ??= userFullName.PrefferedName?.Value;
        user.SurName ??= userFullName.SurName?.Value;
        user.BornName ??= userFullName.BornName?.Value;
        user.FatherName ??= userFullName.FatherName?.Value;

        await UnitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    async Task IUserService.SetVkIfNotSetWithoutAccessChecks(int userId, VkId vkId, AvatarInfo? avatarInfo)
    {
        logger.LogInformation("About to link user: {userId} to {vkId}", userId, vkId);
        var user = await UserRepository.WithProfile(userId);

        user.Extra ??= new UserExtra();

        user.Extra.Vk = $"id{vkId.Value}";
        user.Extra.VkVerified = true;

        await TryAddSocialAvatarImplAsync(avatarInfo, user, "Vkontakte");

        await UnitOfWork.SaveChangesAsync();
    }

    async Task IUserService.SetTelegramIfNotSetWithoutAccessChecks(int userId, TelegramId telegramId, AvatarInfo? avatarInfo)
    {
        logger.LogInformation("About to link user: {userId} to {telegramId}", userId, telegramId);
        var user = await UserRepository.WithProfile(userId);

        user.Extra ??= new UserExtra();
        user.Extra.Telegram = string.IsNullOrWhiteSpace(telegramId.UserName?.Value) ? user.Extra.Telegram : telegramId.UserName;

        await TryAddSocialAvatarImplAsync(avatarInfo, user, "telegram");

        await UnitOfWork.SaveChangesAsync();
    }

    private async Task TryAddSocialAvatarImplAsync(AvatarInfo? avatarInfo, User user, string providerId)
    {
        if (avatarInfo is null)
        {
            return;
        }
        if (
            user.Avatars.FirstOrDefault(ua => ua.IsActive && ua.ProviderId == providerId)
            is UserAvatar oldAvatar)
        {
            if (new Uri(oldAvatar.OriginalUri) != avatarInfo.Uri)
            {
                oldAvatar.IsActive = false;
            }
            else
            {
                return;
            }
        }

        UserAvatar userAvatar = await UploadNewAvatar(avatarInfo.Uri, user, providerId);

        user.SelectedAvatar ??= userAvatar;

        user.Avatars.Add(userAvatar);
    }

    private async Task<UserAvatar> UploadNewAvatar(Uri avatarUri, User user, string providerId)
    {
        var cachedUri = await avatarStorageService.StoreAvatar(avatarUri);

        var userAvatar = new UserAvatar()
        {
            AvatarSource = UserAvatar.Source.SocialNetwork,
            IsActive = true,
            ProviderId = providerId,
            OriginalUri = avatarUri.AbsoluteUri,
            CachedUri = cachedUri?.AbsoluteUri,
            User = user,
        };
        return userAvatar;
    }

    /// <inheritdoc />
    public async Task RemoveVkFromProfile(int userId)
    {
        logger.LogInformation("About to remove VK link from  user: {userId}", userId);
        if (CurrentUserId != userId)
        {
            throw new JoinRpgInvalidUserException();
        }
        var user = await UserRepository.WithProfile(userId);
        user.Extra ??= new UserExtra();
        user.Extra.Vk = null;
        user.Extra.VkVerified = false;

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task RemoveTelegramFromProfile(int userId)
    {
        logger.LogInformation("About to remove Telegram link from  user: {userId}", userId);
        if (CurrentUserId != userId)
        {
            throw new JoinRpgInvalidUserException();
        }
        var user = await UserRepository.WithProfile(userId);
        user.Extra ??= new UserExtra();
        user.Extra.Telegram = null;

        await UnitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc/>
    async Task<AvatarIdentification> IAvatarService.EnsureAvatarPresent(int userId)
    {
        logger.LogInformation("Ensuring that user({userId}) has GrAvatar", userId);

        var user = await UserRepository.WithProfile(userId);

        if (user.SelectedAvatarId is int selectedAvatarId)
        {
            return new AvatarIdentification(selectedAvatarId);
        }

        if (
            user.Avatars.FirstOrDefault(fd => fd.AvatarSource == UserAvatar.Source.GrAvatar && fd.IsActive) is UserAvatar grAvatar
            )
        {
            user.SelectedAvatar = grAvatar;
            await UnitOfWork.SaveChangesAsync();
            return new(grAvatar.UserAvatarId);
        }

        // We do not need to cache avatar here — or GrAvatar will not be automatically updated 
        var userAvatar = new UserAvatar()
        {
            AvatarSource = UserAvatar.Source.GrAvatar,
            IsActive = true,
            ProviderId = null,
            OriginalUri = GravatarHelper.GetLink(user.Email, 64).AbsoluteUri,
            CachedUri = null,
            User = user,
        };
        user.Avatars.Add(userAvatar);

        user.SelectedAvatar ??= userAvatar;

        await UnitOfWork.SaveChangesAsync();

        return new(userAvatar.UserAvatarId);
    }

    async Task IAvatarService.SelectAvatar(int userId, AvatarIdentification avatarIdentification)
    {
        logger.LogInformation("Selecting {avatarId} for user({userId})", avatarIdentification, userId);

        if (CurrentUserId != userId)
        {
            throw new JoinRpgInvalidUserException();
        }

        var user = await UserRepository.WithProfile(userId);
        if (!user.Avatars.Any(a => a.UserAvatarId == avatarIdentification))
        {
            throw new JoinRpgEntityNotFoundException(avatarIdentification, "userAvatar");
        }

        user.SelectedAvatarId = avatarIdentification;

        await UnitOfWork.SaveChangesAsync();
    }

    async Task IAvatarService.DeleteAvatar(int userId, AvatarIdentification avatarIdentification)
    {
        logger.LogInformation("Deleting {avatarId} for user({userId})", avatarIdentification, userId);

        if (CurrentUserId != userId)
        {
            throw new JoinRpgInvalidUserException();
        }

        var user = await UserRepository.WithProfile(userId);
        if (user.Avatars.SingleOrDefault(a => a.UserAvatarId == avatarIdentification)
            is not UserAvatar avatar)
        {
            throw new JoinRpgEntityNotFoundException(avatarIdentification, "userAvatar");
        }

        if (user.SelectedAvatar == avatar)
        {
            throw new InvalidOperationException();
        }

        avatar.IsActive = false;

        await UnitOfWork.SaveChangesAsync();
    }

    async Task IAvatarService.RecacheAvatar(UserIdentification userId, AvatarIdentification avatarIdentification)
    {
        logger.LogInformation("Starting recache of {avatarId} for user({userId})", avatarIdentification, userId);

        if (CurrentUserId != userId && !IsCurrentUserAdmin)
        {
            throw new JoinRpgInvalidUserException();
        }

        var user = await UserRepository.WithProfile(userId);
        if (user.Avatars.SingleOrDefault(a => a.UserAvatarId == avatarIdentification)
            is not UserAvatar avatar)
        {
            throw new JoinRpgEntityNotFoundException(avatarIdentification, "userAvatar");
        }

        var cachedUri = await avatarStorageService.StoreAvatar(new Uri(avatar.OriginalUri));

        avatar.CachedUri = cachedUri?.AbsoluteUri;

        logger.LogInformation("Recache of {avatarId} for user({userId}) completed to {cachedAvatarUri}", avatarIdentification, userId, cachedUri);

        await UnitOfWork.SaveChangesAsync();
    }
}
