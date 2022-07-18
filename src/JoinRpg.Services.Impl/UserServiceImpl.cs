using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl;

/// <inheritdoc />
[UsedImplicitly]
public class UserServiceImpl : DbServiceImplBase, IUserService, IAvatarService
{
    private readonly ILogger<UserServiceImpl> logger;
    private readonly Lazy<IAvatarStorageService> avatarStorageService;

    /// <summary>
    /// ctor
    /// </summary>
    public UserServiceImpl(
        IUnitOfWork unitOfWork,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<UserServiceImpl> logger,
        Lazy<IAvatarStorageService> avatarStorageService
        )
        : base(unitOfWork, currentUserAccessor)
    {
        this.logger = logger;
        this.avatarStorageService = avatarStorageService;
    }

    /// <inheritdoc />
    public async Task UpdateProfile(int userId,
        UserFullName userFullName,
        Gender gender,
        string phoneNumber,
        string nicknames,
        string groupNames,
        string skype,
        string livejournal,
        string telegram,
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
        var tokensToRemove = new[]
          {"http://", "https://", "vk.com", "vkontakte.ru", ".livejournal.com", ".lj.ru", "t.me", "/",};
        user.Extra.Livejournal = livejournal?.RemoveFromString(tokensToRemove);
        user.Extra.Telegram = telegram?.RemoveFromString(tokensToRemove);

        user.Extra.SocialNetworksAccess = socialAccessType;

        await UnitOfWork.SaveChangesAsync();
    }

#nullable enable

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
    async Task IUserService.SetVkIfNotSetWithoutAccessChecks(int userId, VkId vkId, AvatarInfo avatarInfo)
    {
        logger.LogInformation("About to link user: {userId} to {vkId}", userId, vkId);
        var user = await UserRepository.WithProfile(userId);

        user.Extra ??= new UserExtra();
        if (user.Extra.VkVerified)
        {
            logger.LogDebug("Skiping, because user already set VK");
            return;
        }

        user.Extra.Vk = $"id{vkId.Value}";
        user.Extra.VkVerified = true;

        await AddSocialAvatarImplAsync(avatarInfo, user, "Vkontakte");

        await UnitOfWork.SaveChangesAsync();
    }

    async Task IUserService.SetGoogleIfNotSetWithoutAccessChecks(int userId, AvatarInfo avatarInfo)
    {
        logger.LogInformation("About to link user: {userId} to google", userId);
        var user = await UserRepository.WithProfile(userId);

        await AddSocialAvatarImplAsync(avatarInfo, user, "Google");

        await UnitOfWork.SaveChangesAsync();
    }

    private async Task AddSocialAvatarImplAsync(AvatarInfo avatarInfo, User user, string providerId)
    {
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
        var cachedUri = await avatarStorageService.Value.StoreAvatar(avatarUri);

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
        logger.LogInformation("About to remove VK link from  user: {userId}");
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

    /// <inheritdoc/>
    async Task IAvatarService.AddGrAvatarIfRequired(int userId)
    {
        logger.LogInformation("Ensuring that user({userId}) has GrAvatar", userId);

        if (CurrentUserId != userId)
        {
            throw new JoinRpgInvalidUserException();
        }

        var user = await UserRepository.WithProfile(userId);

        if (
            user.Avatars.Any(fd => fd.AvatarSource == UserAvatar.Source.GrAvatar && fd.IsActive)
            )
        {
            logger.LogDebug("GrAvatar already set");
            return;
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

    async Task IAvatarService.RecacheAvatar(int userId, AvatarIdentification avatarIdentification)
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

        var cachedUri = await avatarStorageService.Value.StoreAvatar(new Uri(avatar.OriginalUri));

        avatar.CachedUri = cachedUri?.AbsoluteUri;

        logger.LogInformation("Recache of {avatarId} for user({userId}) completed to {cachedAvatarUri}", avatarIdentification, userId, cachedUri);

        await UnitOfWork.SaveChangesAsync();
    }
}
