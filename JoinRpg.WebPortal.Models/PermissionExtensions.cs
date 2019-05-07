using System;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Filter
{
    public static class PermissionExtensions
    {
        [Pure]
        public static Func<ProjectAcl, bool> GetPermssionExpression(this Permission permission)
        {
            switch (permission)
            {
                case Permission.None:
                    return acl => true;
                case Permission.CanChangeFields:
                    return acl => acl.CanChangeFields;
                case Permission.CanChangeProjectProperties:
                    return acl => acl.CanChangeProjectProperties;
                case Permission.CanGrantRights:
                    return acl => acl.CanGrantRights;
                case Permission.CanManageClaims:
                    return acl => acl.CanManageClaims;
                case Permission.CanEditRoles:
                    return acl => acl.CanEditRoles;
                case Permission.CanManageMoney:
                    return acl => acl.CanManageMoney;
                case Permission.CanSendMassMails:
                    return acl => acl.CanSendMassMails;
                case Permission.CanManagePlots:
                    return acl => acl.CanManagePlots;

                case Permission.CanManageAccommodation:
                    return acl => acl.CanManageAccommodation;
                case Permission.CanSetPlayersAccommodations:
                    return acl => acl.CanSetPlayersAccommodations;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
