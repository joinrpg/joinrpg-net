using System.Threading.Tasks;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces
{
    public interface IUserService
    {
        Task UpdateProfile(int userId, UserFullName userFullName, Gender gender, string phoneNumber, string nicknames, string groupNames, string skype, string livejournal, string telegram, ContactsAccessType socialNetworkAccess);
        Task SetAdminFlag(int userId, bool administratorFlag);
        Task SetVerificationFlag(int userId, bool verificationFlag);
        /// <summary>
        /// Set user name data if not set already.
        /// All access check fortfeit (cause is method typically called during login, so ICurrentUserAccessor could be old).
        /// NOP for verified users
        /// </summary>
        Task SetNameIfNotSetWithoutAccessChecks(int userId, UserFullName userFullName);

        /// <summary>
        /// Set vk Link if not set already.
        /// All access check fortfeit (cause is method typically called during login, so ICurrentUserAccessor could be old).
        /// </summary>
        Task SetVkIfNotSetWithoutAccessChecks(int id, VkId vkId);
        Task RemoveVkFromProfile(int id);
    }
}
