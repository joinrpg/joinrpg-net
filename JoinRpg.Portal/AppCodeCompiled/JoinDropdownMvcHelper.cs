using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JoinRpg.Web.Models.CommonTypes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JoinRpg.Portal.AppCodeCompiled
{
    public static class JoinDropdownMvcHelper
    {
        public static HtmlString JoinFormDropdownFor<TModel, TValue>(
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

            return new HtmlString(
                string.Concat(
                    new HtmlString("<div class=\"form-group\">"),
                    self.LabelFor(expression, htmlAttributes: new {@class = "control-label col-md-2"}),
                    new HtmlString("<div class=\"col-md-10\">"),
                    self.DropDownListFor(expression, select, htmlAttributes: new {@class = "form-control"}),
                    self.DescriptionFor(expression),
                    self.ValidationMessageFor(expression, "", new {@class = "text-danger"}),
                    new HtmlString("</div>"),
                    new HtmlString("</div>")
                ));
        }
    }
}