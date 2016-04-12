using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using JoinRpg.Web.Models;

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

    public enum ShowImplicitGroups
    {
      None, Children, Parents
    }

    public static MvcHtmlString MagicSelectParent<TModel>(this HtmlHelper<TModel> self,
      Expression<Func<TModel, IEnumerable<int>>> expression)
      where TModel : IRootGroupAware
    {
      var container = (IRootGroupAware) ModelMetadata.FromLambdaExpression(m => m, self.ViewData).Model;

      var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);
      var value = metadata.Model as IEnumerable<int>;
      
      string key = ConvertToIdString(value);

      MvcHtmlString magicSelectFor = GetMagicSelect(container.ProjectId, container.RootGroupId, false, false,
        ShowImplicitGroups.Parents, metadata, key);
      return magicSelectFor;
    }

    public static MvcHtmlString MagicSelectFor<TModel>(this HtmlHelper<TModel> self,
      Expression<Func<TModel, IEnumerable<int>>> expression, int projectId, int rootGroupId, bool showCharacters, bool checkCicleForParents, ShowImplicitGroups showGroups)
    {
      var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);
      var value = metadata.Model as IEnumerable<int>;
      string key = ConvertToIdString(value);

      MvcHtmlString magicSelectFor = GetMagicSelect(projectId, rootGroupId, showCharacters, checkCicleForParents, showGroups, metadata, key);
      return magicSelectFor;
    }

    private static string ConvertToIdString(IEnumerable<int> value)
    {
      string key;
      if (value != null)
      {
        key = string.Join(", ", value.Select(id => string.Format("'group^{0}'", id)));
      }
      else
      {
        key = "";
      }

      return key;
    }

    private static MvcHtmlString GetMagicSelect(int projectId, int rootGroupId, bool showCharacters, bool checkCicleForParents, ShowImplicitGroups showGroups, ModelMetadata metadata, string key)
    {
      string implicitGroupsString = GetImplicitGroupString(showGroups);

      // ReSharper disable once UseStringInterpolation we are inside Razor
      var magicSelectFor = MvcHtmlString.Create(string.Format(@"
<div id=""{0}_control"" style=""max-width: 700px;""></div>
<script type=""text/javascript"">
        $(function() {{
        var options = {{
                url: '/' + {1} + '/roles/' + {2} + '/indexjson',
                multiselect: true,
                showcharacters: {3},
                hiddenselect: {{ id: '{0}', name: '{0}' }},
                keyelements: [{4}],
                implicitGroups: {5},
                checkcircles: {6}
            }};
      var c = $('#{0}_control');
      c.multicontrol(options);
    }});
</script>", metadata.PropertyName, projectId, rootGroupId, showCharacters ? "true" : "false", key, implicitGroupsString,
        checkCicleForParents ? "'parents'" : "'none'"));
      return magicSelectFor;
    }

    private static string GetImplicitGroupString(ShowImplicitGroups showGroups)
    {
      string implicitGroupsString;
      switch (showGroups)
      {
        case ShowImplicitGroups.None:
          implicitGroupsString = "'none'";
          break;
        case ShowImplicitGroups.Children:
          implicitGroupsString = "'children'";
          break;
        case ShowImplicitGroups.Parents:
          implicitGroupsString = "'parents'";
          break;
        default:
          throw new ArgumentOutOfRangeException("showGroups", showGroups, null);
      }

      return implicitGroupsString;
    }
  }
}
