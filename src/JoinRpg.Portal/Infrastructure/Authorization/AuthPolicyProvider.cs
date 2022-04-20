using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    internal class AuthPolicyProvider : IAuthorizationPolicyProvider
    {
        private DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public AuthPolicyProvider(IOptions<AuthorizationOptions> options) =>
            // ASP.NET Core only uses one authorization policy provider, so if the custom implementation
            // doesn't handle all policies it should fall back to an alternate provider.
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => FallbackPolicyProvider.GetFallbackPolicyAsync();

        public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(nameof(RequireMaster)))
            {
                var policy = MasterRequirement.TryParsePolicy(policyName);
                if (policy != null)
                {
                    return policy;
                }
            }

            return await FallbackPolicyProvider.GetPolicyAsync(policyName);
        }


        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();
    }
}
