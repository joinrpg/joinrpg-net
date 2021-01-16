using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly]
    public class UserServiceImpl : DbServiceImplBase, IUserService
    {
        public UserServiceImpl(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor) : base(unitOfWork, currentUserAccessor)
        {
        }

        public async Task UpdateProfile(int userId, string surName, string fatherName, string bornName, string prefferedName, Gender gender, string phoneNumber, string nicknames, string groupNames, string skype, string vk, string livejournal, string telegram)
        {
            if (CurrentUserId != userId)
            {
                throw new JoinRpgInvalidUserException();
            }
            var user = await UserRepository.WithProfile(userId);

            if (!user.VerifiedProfileFlag)
            {
                user.SurName = surName;
                user.FatherName = fatherName;
                user.BornName = bornName;
            }
            user.PrefferedName = prefferedName;

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
            user.Extra.Vk = vk?.RemoveFromString(tokensToRemove);
            user.Extra.Telegram = telegram?.RemoveFromString(tokensToRemove);

            await UnitOfWork.SaveChangesAsync();
        }

#nullable enable

        /// <inheritdoc />
        //TODO need to add check on this level [PrincipalPermission(SecurityAction.Demand, Role = Security.AdminRoleName)]
        public async Task SetAdminFlag(int userId, bool administratorFlag)
        {
            var user = await UserRepository.GetById(userId);
            user.Auth.IsAdmin = administratorFlag;
            //TODO: Send email
            await UnitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        //TODO need to add check on this level [PrincipalPermission(SecurityAction.Demand, Role = Security.AdminRoleName)]
        public async Task SetVerificationFlag(int userId, bool verificationFlag)
        {
            var user = await UserRepository.GetById(userId);
            user.VerifiedProfileFlag = verificationFlag;
            //TODO: Send email
            await UnitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task SetNameIfNotSetWithoutAccessChecks(int userId, UserFullName userFullName)
        {
            var user = await UserRepository.WithProfile(userId);

            if (user.VerifiedProfileFlag)
            {
                return;
            }

            user.PrefferedName ??= userFullName.PrefferedName;
            user.SurName ??= userFullName.SurName?.Value;
            user.BornName ??= userFullName.GivenName?.Value;
            user.FatherName ??= userFullName.FatherName?.Value;

            await UnitOfWork.SaveChangesAsync();
        }
    }
}
