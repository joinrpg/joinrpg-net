using System;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public static class AuthorizationBuilderConvienceMethods
    {
        private const string RequirementSuffix = "Requirement";

        public static AuthorizationOptions AddPolicyAsRequirement<TRequirement>(this AuthorizationOptions options)
            where TRequirement: IAuthorizationRequirement, new()

        {
            var type = typeof(TRequirement);
            if (!type.Name.EndsWith(RequirementSuffix))
            {
                throw new ArgumentException();
            }
            var name = type.Name.Substring(0, type.Name.Length - RequirementSuffix.Length);
            options.AddPolicy(name, policy => policy.AddRequirements(new TRequirement()));
            return options;
        }
    }
}
