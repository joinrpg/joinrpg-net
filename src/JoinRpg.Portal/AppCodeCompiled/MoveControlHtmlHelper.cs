using System.Linq.Expressions;
using JetBrains.Annotations;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JoinRpg.Portal;

public static class MoveControlHtmlHelper
{

    public static IHtmlContent MoveControl<TModel, TValue>(this IHtmlHelper<TModel> self,
      Expression<Func<TModel, TValue>> expression, [AspMvcAction] string actionName,
      [AspMvcController] string? controllerName, int? parentObjectId = null)
        where TValue : IMovableListItem
    {
        var item = (IMovableListItem)self.GetValue(expression);

        var paramters = new MoveControlParametersViewModel()
        {
            Item = item,
            ActionName = actionName,
            ControllerName = controllerName,
            ProjectId = item.ProjectId,
            ListItemId = item.ItemId,
            ParentObjectId = parentObjectId ?? item.ProjectId
        };

        return self.Partial("_MoveControlPartial", paramters);
    }

    public static IHtmlContent MoveControl<TModel, TValue>(this IHtmlHelper<TModel> self,
      [InstantHandle] Expression<Func<TModel, TValue>> expression, [AspMvcAction] string actionName)
        where TValue : IMovableListItem
    {
        var currentController = GetRequiredString(self.ViewContext.RouteData, "controller");
        return self.MoveControl(expression, actionName, currentController);
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
