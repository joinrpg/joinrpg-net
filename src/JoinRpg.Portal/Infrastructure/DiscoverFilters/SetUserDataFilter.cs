using JoinRpg.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure;

public class SetUserDataFilterAttribute : ResultFilterAttribute
{
    public SetUserDataFilterAttribute(ICurrentUserAccessor currentUserAccessor) => CurrentUserAccessor = currentUserAccessor;

    private ICurrentUserAccessor CurrentUserAccessor { get; }

    /// <inheritedoc />
    public override Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ViewResult viewResult && CurrentUserAccessor.UserIdOrDefault != null)
        {
            viewResult.ViewData["UserDisplayName"] = CurrentUserAccessor.DisplayName;
            viewResult.ViewData["UserAvatar"] = CurrentUserAccessor.Avatar;
        }

        return base.OnResultExecutionAsync(context, next);
    }
}
