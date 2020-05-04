using System;
using System.Threading.Tasks;
using JoinRpg.Helpers.Web;
using JoinRpg.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure
{
    public class SetUserDataFilterAttribute : ResultFilterAttribute
    {
        public SetUserDataFilterAttribute(ICurrentUserAccessor currentUserAccessor) => CurrentUserAccessor = currentUserAccessor;

        private ICurrentUserAccessor CurrentUserAccessor { get; }

        /// <inheritedoc />
        public override Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var controller = context.Controller as Controller;
            controller.ViewBag.IsProduction = context.HttpContext.Request.Host.Host == "joinrpg.ru";

            if (CurrentUserAccessor.UserIdOrDefault != null)
            {
                controller.ViewBag.UserDisplayName = CurrentUserAccessor.DisplayName;
                controller.ViewBag.GravatarHash = CurrentUserAccessor.Email.GravatarHash().Trim();
            }

            return base.OnResultExecutionAsync(context, next);
        }
    }
}
