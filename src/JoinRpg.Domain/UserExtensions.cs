using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
    public static class UserExtensions
    {
        public static IEnumerable<Project> GetProjects(this User user, Func<ProjectAcl, bool> predicate) => user.ProjectAcls.Where(predicate).Select(acl => acl.Project);

        public static AccessReason GetProfileAccess([NotNull] this User user, [CanBeNull] User? currentUser)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (currentUser == null)
            {
                return AccessReason.NoAccess;
            }
            if (user.UserId == currentUser.UserId)
            {
                return AccessReason.ItsMe;
            }
            if (user.Claims.Any(claim => claim.HasAccess(currentUser.UserId) && claim.ClaimStatus != Claim.Status.AddedByMaster))
            {
                return AccessReason.Master;
            }
            if (user.ProjectAcls.Any(acl => acl.Project.HasMasterAccess(currentUser.UserId)))
            {
                return AccessReason.CoMaster;
            }
            if (currentUser.Auth.IsAdmin == true)
            {
                return AccessReason.Administrator;
            }
            return AccessReason.NoAccess;
        }

        public static ContactsAccessType GetSocialNetworkAccess(this User user) => user.Extra?.SocialNetworksAccess ?? ContactsAccessType.OnlyForMasters;

        public enum AccessReason
        {
            NoAccess,
            ItsMe,
            Master,
            CoMaster,
            Administrator,
        }

        /// <summary>
        /// Returns display name of a user
        /// </summary>
        [NotNull]
        public static string GetDisplayName([NotNull] this User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!string.IsNullOrWhiteSpace(user.PrefferedName))
            {
                return user.PrefferedName;
            }
            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                return user.FullName;
            }
            return user.Email.TakeWhile(ch => ch != '@').AsString();
        }

        /// <summary>
        /// Returns hint for a user
        /// </summary>
        [NotNull]
        public static string GetHint([NotNull] this User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var result = "";
            if (user.VerifiedProfileFlag)
            {
                result += "Подтвержденный пользователь";
            }

            return result;
        }
    }
}
