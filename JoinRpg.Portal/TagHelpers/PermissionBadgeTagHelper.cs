using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JoinRpg.Portal.TagHelpers
{
    // You may need to install the Microsoft.AspNetCore.Razor.Runtime package into your project
    [HtmlTargetElement("permission-badge")]
    public class PermissionBadgeTagHelper : TagHelper
    {
        private HtmlHelper _htmlHelper;
        private HtmlEncoder _htmlEncoder;

        public PermissionBadgeTagHelper(IHtmlHelper htmlHelper, HtmlEncoder htmlEncoder)
        {
            _htmlHelper = htmlHelper as HtmlHelper;
            _htmlEncoder = htmlEncoder;
        }

        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; }

        [ViewContext]
        public ViewContext ViewContext
        {
            set { _htmlHelper.Contextualize(value); }
        }


        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;

            var partialView = await _htmlHelper.PartialAsync("Acl/PermissionBadge", For);

            var writer = new StringWriter();
            partialView.WriteTo(writer, _htmlEncoder);

            output.Content.SetHtmlContent(writer.ToString());
        }
    }
}
