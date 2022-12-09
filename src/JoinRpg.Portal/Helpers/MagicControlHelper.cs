using JoinRpg.Helpers;
using Microsoft.AspNetCore.Html;

namespace JoinRpg.Web.Helpers;

public enum ShowImplicitGroups
{
    None,
    Children,
    Parents,
}

public enum MagicControlStrategy
{
    Changer,
    NonChanger,
}


public static class MagicControlHelper
{
    private static string GetImplicitGroupString(ShowImplicitGroups showGroups)
    {
        return showGroups switch
        {
            ShowImplicitGroups.None => "'none'",
            ShowImplicitGroups.Children => "'children'",
            ShowImplicitGroups.Parents => "'parents'",
            _ => throw new ArgumentOutOfRangeException(nameof(showGroups), showGroups, null),
        };
    }

    public static HtmlString GetMagicSelect(int projectId, bool showCharacters, ShowImplicitGroups showGroups,
        MagicControlStrategy strategy, string propertyName, IEnumerable<string> elements, bool showSpecial)
    {
        var implicitGroupsString = GetImplicitGroupString(showGroups);

        var strategyString = GetStrategyString(strategy);

        //TODO: convert to verbatim
        var magicSelectFor = new HtmlString(string.Format(@"
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
            strategyString, elements.JoinStrings(", "), Random.Shared.Next(), showSpecial ? "json_full" : "json_real"));
        return magicSelectFor;
    }

    private static string GetStrategyString(MagicControlStrategy strategy)
    {
        return strategy switch
        {
            MagicControlStrategy.Changer => "changer",
            MagicControlStrategy.NonChanger => "nonchanger",
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null),
        };
    }
}
