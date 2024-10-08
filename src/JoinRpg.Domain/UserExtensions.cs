using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Domain;

public static class UserExtensions
{
    public static IEnumerable<Project> GetProjects(this User user, Func<ProjectAcl, bool> predicate) => user.ProjectAcls.Where(predicate).Select(acl => acl.Project);

    public static AccessReason GetProfileAccess(this User user, User? currentUser)
    {
        ArgumentNullException.ThrowIfNull(user);

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

    public static UserFullName ExtractFullName(this User user)
    {
        return new UserFullName(
            PrefferedName.FromOptional(user.PrefferedName),
            BornName.FromOptional(user.BornName),
            SurName.FromOptional(user.SurName),
            FatherName.FromOptional(user.FatherName));
    }

    public static UserDisplayName ExtractDisplayName(this User user)
    {
        return UserDisplayName.Create(user.ExtractFullName(), new Email(user.Email));
    }

    /// <summary>
    /// Returns display name of a user
    /// </summary>
    public static string GetDisplayName(this User user)
    {
        return user.ExtractDisplayName().DisplayName;
    }

    /// <summary>
    /// Returns hint for a user
    /// </summary>
    public static string GetHint(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var result = "";
        if (user.VerifiedProfileFlag)
        {
            result += "Подтвержденный пользователь";
        }

        return result;
    }
}
