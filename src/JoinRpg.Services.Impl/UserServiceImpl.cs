using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl
{
    /// <inheritdoc />
    [UsedImplicitly]
    public class UserServiceImpl : DbServiceImplBase, IUserService
    {
        private readonly ILogger<UserServiceImpl> logger;

        /// <summary>
        /// ctor
        /// </summary>
        public UserServiceImpl(
            IUnitOfWork unitOfWork,
            ICurrentUserAccessor currentUserAccessor,
            ILogger<UserServiceImpl> logger
            )
            : base(unitOfWork, currentUserAccessor) => this.logger = logger;

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
                user.BornName = userFullName.GivenName?.Value;
            }
            user.PrefferedName = userFullName.PrefferedName;

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

            user.PrefferedName ??= userFullName.PrefferedName;
            user.SurName ??= userFullName.SurName?.Value;
            user.BornName ??= userFullName.GivenName?.Value;
            user.FatherName ??= userFullName.FatherName?.Value;

            await UnitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task SetVkIfNotSetWithoutAccessChecks(int userId, VkId vkId)
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

            await UnitOfWork.SaveChangesAsync();
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
    }
}
