using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Portal.Infrastructure
{
    // Purpose is to send CSRF token to JS-allowed cookie to allow APIs to use it
    // see https://remibou.github.io/CSRF-protection-with-ASPNET-Core-and-Blazor-Week-29/
    public class CsrfTokenCookieMiddleware
    {
        private readonly RequestDelegate _next;

        public CsrfTokenCookieMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
        {
            var token = antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append("CSRF-TOKEN", token.RequestToken!, new CookieOptions { HttpOnly = false });

            await _next(context);
        }
    }
}
