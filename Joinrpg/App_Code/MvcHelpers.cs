using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace JoinRpg.Web.App_Code
{
  public static class MvcHtmlHelpers
  {
    public static MvcHtmlString DescriptionFor<TModel, TValue>(this HtmlHelper<TModel> self,
      Expression<Func<TModel, TValue>> expression)
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

    public static MvcHtmlString MagicSelectFor<TModel, TValue>(this HtmlHelper<TModel> self,
      Expression<Func<TModel, TValue>> expression, int projectId, int rootGroupId, bool showCharacters)
    {
      var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);
      var value = metadata.Model as IEnumerable<int>;

      // ReSharper disable once UseStringInterpolation
      string key;
      if (value != null)
      {
        key = string.Join(", ", value.Select(id => string.Format("'{0}'", id)));
      }
      else
      {
        key = "";
      }

      // ReSharper disable once UseStringInterpolation we are inside Razor
      return MvcHtmlString.Create(string.Format(@"
<div id=""{0}_control"" style=""max-width: 700px;""></div>
<script type=""text/javascript"">
        $(function() {{
        var options = {{
                url: '/' + {1} + '/roles/' + {2} + '/indexjson',
                multiselect: true,
                showcharacters: {3},
                hiddenselect: {{ id: '{0}', name: '{0}' }},
                keyelements: [{4}]
            }};
      var c = $('#{0}_control');
      c.multicontrol(options);
    }});
</script>", metadata.PropertyName, projectId, rootGroupId, showCharacters ? "true" : "false", key));
    }
  }
}
