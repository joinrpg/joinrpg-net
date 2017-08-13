using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using JoinRpg.Helpers;

namespace JoinRpg.Web
{
  public static class SearchableDropdownMvcHelper
  {
    public static MvcHtmlString SearchableDropdownFor<TModel, TValue>(this HtmlHelper<TModel> self,
      Expression<Func<TModel, TValue>> expression, IEnumerable<ImprovedSelectListItem> items)
    {
      //TODO: selected value from model
      var itemsString =
        string.Join("\n",
          items.Select(item => string.Format(
            "<option data-tokens=\"{0} {1} {3}\" data-subtext=\"{1}\" value=\"{2}\">{3}</option >",
            item.ExtraSearch, item.Subtext, item.Value, item.Text)));

      // ReSharper disable once UseStringInterpolation we are inside Razor
      return MvcHtmlString.Create(string.Format(@"  <select 
    class=""selectpicker""
      data-live-search = ""true""
      data-live-search-normalize = ""true""
      data-size = ""10""
      name=""{1}"">
        {0}
      </select>", itemsString, expression.AsPropertyName()));
    }
  }

  public class ImprovedSelectListItem : SelectListItem
  {
    public object ExtraSearch { get; set; }
    public object Subtext { get; set; }
  }
}