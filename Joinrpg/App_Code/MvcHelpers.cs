using System;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace JoinRpg.Web.App_Code
{
  public static class MvcHtmlHelpers
  {
    public static MvcHtmlString DescriptionFor<TModel, TValue>(this HtmlHelper<TModel> self, Expression<Func<TModel, TValue>> expression)
    {
      var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);
      var description = metadata.Description;

      if (string.IsNullOrWhiteSpace(description))
      {
        return MvcHtmlString.Empty;
      }

      // ReSharper disable once UseStringInterpolation we are inside Razor
      return MvcHtmlString.Create(string.Format(@"<div class=""help-block"">{0}</div>", description));
    }
  }
}
