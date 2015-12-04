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

      return
        // ReSharper disable once UseStringInterpolation we are inside Razor
        MvcHtmlString.Create(string.Format(@"<span class=""input-group-addon""><span class=""glyphicon glyphicon-info-sign"" title=""{0}""></span></span>",
          description));
    }
  }
}
