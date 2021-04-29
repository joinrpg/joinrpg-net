using JoinRpg.Portal.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Controllers
{
    [ApiController]
    public class LetsEncryptController : ControllerBase
    {
        private string redirectUrl;
        public LetsEncryptController(IOptions<LetsEncryptOptions> options) => redirectUrl = options.Value.RedirectUrl;

        [HttpGet("/.well-known/acme-challenge/{token}")]
        [AllowAnonymous]
        public IActionResult RedirectLets(string token)
        {
            if (redirectUrl is null)
            {
                return StatusCode(500, "Set Redirect data");
            }
            return Redirect(redirectUrl + token);
        }
    }
}
