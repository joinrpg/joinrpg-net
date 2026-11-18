using System.Linq.Expressions;
using JoinRpg.Web.Models.CommonTypes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JoinRpg.Portal.AppCodeCompiled;

public static class JoinDropdownMvcHelper
{
    [Obsolete("Надо использовать MasterSelector/CharacterSelector/CharacterGroupSelector или сделать свою поверх TypedSelector")]
    public static IHtmlContent DropDownListFor<TModel, TValue>(
        this IHtmlHelper<TModel> self,
        Expression<Func<TModel, TValue>> expression,
        IEnumerable<JoinSelectListItem> select)
        => self.DropDownListFor(expression, select.ToSelectListItems(), new { @class = "form-control" });

    private static IEnumerable<SelectListItem> ToSelectListItems(this IEnumerable<JoinSelectListItem> items) =>
        items.Select(item => new SelectListItem()
        {
            Text = item.Text,
            Value = item.Value.ToString(),
            Selected = item.Selected,
        });
}
