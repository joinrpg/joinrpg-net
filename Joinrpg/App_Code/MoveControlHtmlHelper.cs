using System;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using JetBrains.Annotations;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.App_Code
{
  public static class MoveControlHtmlHelper
  {

    public static MvcHtmlString MoveControl<TModel, TValue>(this HtmlHelper<TModel> self,
      Expression<Func<TModel, TValue>> expression, [AspMvcAction] string actionName,
      [AspMvcController] string controllerName, int? parentObjectId = null)
    {
      var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);

      var item = (IMovableListItem) metadata.Model;

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

    public static MvcHtmlString MoveControl<TModel, TValue>(this HtmlHelper<TModel> self,
      [InstantHandle] Expression<Func<TModel, TValue>> expression, [AspMvcAction] string actionName)
    {
      var rd = self.ViewContext.RouteData;
      string currentController = rd.GetRequiredString("controller");
      return self.MoveControl(expression, actionName, currentController);
    }
  }
}