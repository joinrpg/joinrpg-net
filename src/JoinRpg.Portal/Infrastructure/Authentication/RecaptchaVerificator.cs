using System.Net;
using System.Threading.Tasks;
using BitArmory.ReCaptcha;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;


namespace JoinRpg.Portal.Infrastructure.Authentication
{
    [UsedImplicitly]
    public class RecaptchaVerificator : IRecaptchaVerificator
    {
        private readonly ReCaptchaService reCaptchaService;
        private readonly IOptions<RecaptchaOptions> recaptchaOptions;

        public RecaptchaVerificator(ReCaptchaService reCaptchaService, IOptions<RecaptchaOptions> recaptchaOptions)
        {
            this.reCaptchaService = reCaptchaService;
            this.recaptchaOptions = recaptchaOptions;
        }
        public Task<bool> ValidateToken(string recaptchaToken, IPAddress clientIp)
        {
            //this can be null i.e. under proxy or from localhost.
            //TODO IISIntegration, etc
            var secret = recaptchaOptions.Value.PrivateKey;

            return reCaptchaService.Verify2Async(recaptchaToken, clientIp?.ToString(), secret);
        }

        public bool IsRecaptchaConfigured()
        {
            return IsRecaptchaKeyValid(recaptchaOptions.Value.PublicKey)
                && IsRecaptchaKeyValid(recaptchaOptions.Value.PrivateKey);
        }

        private bool IsRecaptchaKeyValid(string key)
        {
            return !string.IsNullOrEmpty(key) && !string.Equals(key, "_");
        }
    }
}
