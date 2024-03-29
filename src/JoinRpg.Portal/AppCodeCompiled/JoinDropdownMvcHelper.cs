using System.Linq.Expressions;
using Joinrpg.AspNetCore.Helpers;
using JoinRpg.Web.Models.CommonTypes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JoinRpg.Portal.AppCodeCompiled;

public static class JoinDropdownMvcHelper
{
    public static IHtmlContent JoinFormDropdownFor<TModel, TValue>(
        this IHtmlHelper<TModel> self,
        Expression<Func<TModel, TValue>> expression,
        IEnumerable<JoinSelectListItem> items)
    {
        var select = items.Select(item => new SelectListItem()
        {
            Text = item.Text,
            Value = item.Value.ToString(),
            Selected = item.Selected,
        });

        return self.JoinList("",
                new HtmlString("<div class=\"form-group\">"),
                self.LabelFor(expression, htmlAttributes: new { @class = "control-label col-md-2" }),
                new HtmlString("<div class=\"col-md-10\">"),
                self.DropDownListFor(expression, select, htmlAttributes: new { @class = "form-control" }),
                self.DescriptionFor(expression),
                self.ValidationMessageFor(expression, "", new { @class = "text-danger" }),
                new HtmlString("</div>"),
                new HtmlString("</div>")
            );

    }
    public static IHtmlContent DropDownListFor<TModel, TValue>(
        this IHtmlHelper<TModel> self,
        Expression<Func<TModel, TValue>> expression,
        IEnumerable<JoinSelectListItem> select)
        => self.DropDownListFor(expression, select.ToSelectListItems(), new { @class = "form-control" });

    public static IEnumerable<SelectListItem> ToSelectListItems(this IEnumerable<JoinSelectListItem> items) =>
        items.Select(item => new SelectListItem()
        {
            Text = item.Text,
            Value = item.Value.ToString(),
            Selected = item.Selected,
        });
}
