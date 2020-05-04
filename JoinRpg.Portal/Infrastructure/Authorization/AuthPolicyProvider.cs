using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    internal class AuthPolicyProvider : IAuthorizationPolicyProvider
    {
        private DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public AuthPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            // ASP.NET Core only uses one authorization policy provider, so if the custom implementation
            // doesn't handle all policies it should fall back to an alternate provider.
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(nameof(RequireMaster)))
            {
                AuthorizationPolicy policy = MasterRequirement.TryParsePolicy(policyName);
                if (policy != null)
                {
                    return Task.FromResult(policy);
                }
            }

            return FallbackPolicyProvider.GetPolicyAsync(policyName);
        }


        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();
    }
}
