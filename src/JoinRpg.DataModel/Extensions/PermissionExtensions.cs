using System.Diagnostics.Contracts;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DataModel.Extensions;

public static class PermissionExtensions
{
    [Pure]
    public static Func<ProjectAcl, bool> GetPermssionExpression(this Permission permission)
    {
        return permission switch
        {
            Permission.None => acl => true,
            Permission.CanChangeFields => acl => acl.CanChangeFields,
            Permission.CanChangeProjectProperties => acl => acl.CanChangeProjectProperties,
            Permission.CanGrantRights => acl => acl.CanGrantRights,
            Permission.CanManageClaims => acl => acl.CanManageClaims,
            Permission.CanEditRoles => acl => acl.CanEditRoles,
            Permission.CanManageMoney => acl => acl.CanManageMoney,
            Permission.CanSendMassMails => acl => acl.CanSendMassMails,
            Permission.CanManagePlots => acl => acl.CanManagePlots,
            Permission.CanManageAccommodation => acl => acl.CanManageAccommodation,
            Permission.CanSetPlayersAccommodations => acl => acl.CanSetPlayersAccommodations,
            _ => throw new ArgumentOutOfRangeException(nameof(permission)),
        };
    }

    [Pure]
    public static Permission[] GetPermissions(this ProjectAcl acl)
    {
        return Impl().ToArray();

        IEnumerable<Permission> Impl()
        {
            foreach (var permission in Enum.GetValues<Permission>())
            {
                if (permission.GetPermssionExpression()(acl))
                {
                    yield return permission;
                }
            }
        }
    }

    public static void SetPermissions(this ProjectAcl acl, IReadOnlyCollection<Permission> permissions)
    {
        bool Has(Permission p) => permissions.Contains(p);

        acl.CanChangeFields = Has(Permission.CanChangeFields);
        acl.CanChangeProjectProperties = Has(Permission.CanChangeProjectProperties);
        acl.CanGrantRights = Has(Permission.CanGrantRights);
        acl.CanManageClaims = Has(Permission.CanManageClaims);
        acl.CanEditRoles = Has(Permission.CanEditRoles);
        acl.CanManageMoney = Has(Permission.CanManageMoney);
        acl.CanSendMassMails = Has(Permission.CanSendMassMails);
        acl.CanManagePlots = Has(Permission.CanManagePlots);
        acl.CanManageAccommodation = Has(Permission.CanManageAccommodation) && acl.Project.Details.EnableAccommodation;
        acl.CanSetPlayersAccommodations = Has(Permission.CanSetPlayersAccommodations) && acl.Project.Details.EnableAccommodation;
    }
}
