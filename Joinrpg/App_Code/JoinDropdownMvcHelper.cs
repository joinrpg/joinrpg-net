using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.App_Code
{
    public static class JoinDropdownMvcHelper
    {
        public static MvcHtmlString JoinFormDropdownFor<TModel, TValue>(
            this HtmlHelper<TModel> self,
            Expression<Func<TModel, TValue>> expression,
            IEnumerable<JoinSelectListItem> items)
        {
            var select = items.Select(item => new SelectListItem()
            {
                Text = item.Text,
                Value = item.Value.ToString(),
                Selected = item.Selected,
            });

            return MvcHtmlString.Create(
                string.Concat(
                    new MvcHtmlString("<div class=\"form-group\">"),
                    self.LabelFor(expression, htmlAttributes: new { @class = "control-label col-md-2" }),
                    new MvcHtmlString("<div class=\"col-md-10\">"),
                    self.DropDownListFor(expression, select, htmlAttributes: new { @class = "form-control" } ),
                    self.DescriptionFor(expression),
                    self.ValidationMessageFor(expression, "", new { @class = "text-danger" }),
                    new MvcHtmlString("</div>"),
                    new MvcHtmlString("</div>")
                ));
        }
    }
}
