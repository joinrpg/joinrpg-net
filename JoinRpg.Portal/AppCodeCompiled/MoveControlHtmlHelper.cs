using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Joinrpg.AspNetCore.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JoinRpg.Portal
{
    public static class MoveControlHtmlHelper
    {

        public static IHtmlContent MoveControl<TModel, TValue>(this IHtmlHelper<TModel> self,
          Expression<Func<TModel, TValue>> expression, [AspMvcAction] string actionName,
          [AspMvcController] string controllerName, int? parentObjectId = null)
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
        {
            var rd = self.ViewContext.RouteData;
            string currentController = rd.GetRequiredString("controller");
            return self.MoveControl(expression, actionName, currentController);
        }
    }
}
