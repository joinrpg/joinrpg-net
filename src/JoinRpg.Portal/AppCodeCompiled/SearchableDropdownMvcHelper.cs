using System.Linq.Expressions;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.CommonTypes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JoinRpg.Portal;

public static class SearchableDropdownMvcHelper
{
    [Obsolete]
    public static IHtmlContent SearchableDropdownFor<TModel, TValue>(
        this IHtmlHelper<TModel> self,
        Expression<Func<TModel, TValue>> expression,
        IEnumerable<ImprovedSelectListItem> items)
    {
        //TODO: selected value from model
        var itemsString =
            string.Join("\n",
                items.Select(item => string.Format(
                    "<option data-tokens=\"{0} {1} {3}\" data-subtext=\"{1}\" value=\"{2}\">{3}</option >",
                    item.ExtraSearch,
                    item.Subtext,
                    item.Value,
                    item.Text)));

        return new HtmlString(string.Format(@"  <select 
    class=""selectpicker""
      data-live-search = ""true""
      data-live-search-normalize = ""true""
      data-size = ""10""
      name=""{1}"">
        {0}
      </select>",
            itemsString,
            expression.AsPropertyName()));
    }

    [Obsolete]
    public static IHtmlContent SearchableDropdown(this IHtmlHelper self,
        string name,
        IEnumerable<ImprovedSelectListItem> items)
    {
        //TODO: selected value from model
        var itemsString =
            string.Join("\n",
                items.Select(item => string.Format(
                    "<option data-tokens=\"{0} {1} {3}\" data-subtext=\"{1}\" value=\"{2}\">{3}</option >",
                    item.ExtraSearch,
                    item.Subtext,
                    item.Value,
                    item.Text)));

        // ReSharper disable once UseStringInterpolation we are inside Razor
        return new HtmlString(string.Format(@"  <select 
    class=""selectpicker""
      data-live-search = ""true""
      data-live-search-normalize = ""true""
      data-size = ""10""
      name=""{1}"">
        {0}
      </select>",
            itemsString,
            name));
    }
}


