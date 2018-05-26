using JoinRpg.Helpers;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace JoinRpg.Web.Helpers
{
  public enum ShowImplicitGroups
  {
    None, Children, Parents,
  }

  public enum MagicControlStrategy
  {
    Changer, NonChanger,
  }


  public static class MagicControlHelper
  {
    private static readonly Random Random = new Random();


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
          throw new ArgumentOutOfRangeException(nameof(showGroups), showGroups, null);
      }

      return implicitGroupsString;
    }

    public static MvcHtmlString GetMagicSelect(int projectId, bool showCharacters, ShowImplicitGroups showGroups, MagicControlStrategy strategy, string propertyName, IEnumerable<string> elements, bool showSpecial)
    {
      var implicitGroupsString = GetImplicitGroupString(showGroups);

      var strategyString = GetStrategyString(strategy);

      //TODO: convert to verbatim
      var magicSelectFor = MvcHtmlString.Create(string.Format(@"
<div id=""{0}_control_{6}"" style=""max-width: 700px;width:100%""></div>
<script type=""text/javascript"">
        $(function() {{
        var options = 
          {{
            url: '/' + {1} + '/roles/{7}',
            multiselect: true,
            showcharacters: {2},
            hiddenselect: {{ id: '{0}', name: '{0}' }},
            implicitgroups: {3},
            strategy: {{
              type: '{4}',
              elements: [{5}]
            }},
          }};
      $('#{0}_control_{6}').multicontrol(options);
    }});
</script>", propertyName, projectId, showCharacters ? "true" : "false", implicitGroupsString,
        strategyString, elements.JoinStrings(", "), Random.Next(), showSpecial ? "json_full" : "json_real"));
      return magicSelectFor;
    }

    private static string GetStrategyString(MagicControlStrategy strategy)
    {
      switch (strategy)
      {
        case MagicControlStrategy.Changer:
          return "changer";
        case MagicControlStrategy.NonChanger:
          return "nonchanger";
        default:
          throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
      }
    }
  }
}
