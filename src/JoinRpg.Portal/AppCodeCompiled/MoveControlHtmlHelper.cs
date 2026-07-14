using System.Linq.Expressions;
using JoinRpg.Web.Models;
using JoinRpg.Web.ProjectCommon.Fields;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JoinRpg.Portal;

public static class MoveControlHtmlHelper
{
    [Obsolete]
    public static async Task<IHtmlContent> MoveControlAsync<TModel, TValue>(this IHtmlHelper<TModel> self,
      Expression<Func<TModel, TValue>> expression, string actionName,
      string? controllerName, int? parentObjectId = null)
        where TValue : IMoveableNonInteractiveListItem
    {
        var item = (IMoveableNonInteractiveListItem)self.GetValue(expression);

        var paramters = new MoveControlParametersViewModel()
        {
            Item = item,
            ActionName = actionName,
            ControllerName = controllerName,
            ProjectId = item.ProjectId,
            ListItemId = item.ItemId,
            ParentObjectId = parentObjectId ?? item.ProjectId
        };

        return await self.PartialAsync("_MoveControlPartial", paramters);
    }

    [Obsolete]
    public static async Task<IHtmlContent> MoveControlAsync<TModel, TValue>(this IHtmlHelper<TModel> self,
      Expression<Func<TModel, TValue>> expression, string actionName)
        where TValue : IMoveableNonInteractiveListItem
    {
        var currentController = GetRequiredString(self.ViewContext.RouteData, "controller");
        return await self.MoveControlAsync(expression, actionName, currentController);
    }

    private static string? GetRequiredString(RouteData routeData, string keyName)
    {
        if (!routeData.Values.TryGetValue(keyName, out var value))
        {
            throw new InvalidOperationException($"Could not find key with name '{keyName}'");
        }

        return value?.ToString();
    }
}
