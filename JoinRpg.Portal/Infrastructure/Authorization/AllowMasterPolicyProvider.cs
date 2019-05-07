using System;
using System.Threading.Tasks;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    internal class AllowMasterPolicyProvider : IAuthorizationPolicyProvider
    {
        private DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public AllowMasterPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            // ASP.NET Core only uses one authorization policy provider, so if the custom implementation
            // doesn't handle all policies it should fall back to an alternate provider.
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        // Policies are looked up by string name, so expect 'parameters' (like age)
        // to be embedded in the policy names. This is abstracted away from developers
        // by the more strongly-typed attributes derived from AuthorizeAttribute
        // (like [MinimumAgeAuthorize()] in this sample)
        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(PolicyConstants.AllowMasterPolicyPrefix, StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<Permission>(policyName.Substring(PolicyConstants.AllowMasterPolicyPrefix.Length), out var permission))
            {
                var policy = new AuthorizationPolicyBuilder();
                policy.AddRequirements(new AllowMasterRequirement { Permission = permission });
                return Task.FromResult(policy.Build());
            }

            return FallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();
    }
}
