using JoinRpg.Helpers;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace JoinRpg.Web.Helpers
{
  public enum ShowImplicitGroups
  {
    None, Children, Parents
  }

  public enum MagicControlStrategy
  {
    Changer, NonChanger
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

    public static MvcHtmlString GetMagicSelect(int projectId, int rootGroupId, bool showCharacters, ShowImplicitGroups showGroups, MagicControlStrategy strategy, string propertyName, IEnumerable<string> elements)
    {
      var implicitGroupsString = GetImplicitGroupString(showGroups);

      var strategyString = GetStrategyString(strategy);

      //TODO: convert to verbatim
      var magicSelectFor = MvcHtmlString.Create(string.Format(@"
<div id=""{0}_control_{7}"" style=""max-width: 700px;width:100%""></div>
<script type=""text/javascript"">
        $(function() {{
        var options = 
          {{
            url: '/' + {1} + '/roles/' + {2} + '/indexjson',
            multiselect: true,
            showcharacters: {3},
            hiddenselect: {{ id: '{0}', name: '{0}' }},
            implicitgroups: {4},
            strategy: {{
              type: '{5}',
              elements: [{6}]
            }},
          }};
      $('#{0}_control_{7}').multicontrol(options);
    }});
</script>", propertyName, projectId, rootGroupId, showCharacters ? "true" : "false", implicitGroupsString,
        strategyString, elements.Join(", "), Random.Next()));
      return magicSelectFor;
    }

    private static string GetStrategyString(MagicControlStrategy strategy)
    {
      string strategyString;
      switch (strategy)
      {
        case MagicControlStrategy.Changer:
          strategyString
            = "changer";
          break;
        case MagicControlStrategy.NonChanger:
          strategyString
            = "nonchanger";
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
      }
      return strategyString;
    }
  }
}
