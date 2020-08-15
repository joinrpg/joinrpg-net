using System;
using System.Linq;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public class MasterRequirement : IAuthorizationRequirement
    {
        public Permission Permission { get; set; }

        public bool AllowPublish { get; set; }

        public bool AllowAdmin { get; set; }

        public static AuthorizationPolicy TryParsePolicy(string policyName)
        {
            var array = policyName.Split("__");
            var (policyType, permissionString) = (array.FirstOrDefault(), array.Skip(1).FirstOrDefault());
            if (string.IsNullOrWhiteSpace(policyType) || string.IsNullOrWhiteSpace(permissionString))
            {
                return null;
            }
            var requirement = new MasterRequirement();
            switch (policyType)
            {
                case nameof(RequireMasterOrAdmin):
                    requirement.AllowAdmin = true;
                    break;
                case nameof(RequireMasterOrPublish):
                    requirement.AllowPublish = true;
                    break;
                case nameof(RequireMaster):
                    break;
                default:
                    return null;
            }

            if (!Enum.TryParse<Permission>(permissionString, out var permission))
            {
                return null;
            }

            requirement.Permission = permission;

            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(requirement);

            return policy.Build();
        }
    }
}
