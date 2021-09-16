using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JoinRpg.Portal.TagHelpers
{
    // TODO test and enable [HtmlTargetElement("button")]
    [HtmlTargetElement("a")]
    [HtmlTargetElement("li")] //for bootstrap menus
    public class ConditionalDisableTagHelper : TagHelper
    {
        [HtmlAttributeName("asp-is-disabled")]
        public bool IsDisabled { set; get; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (IsDisabled)
            {
                output.AddClass("disabled", HtmlEncoder.Default);
            }
            base.Process(context, output);
            if (IsDisabled && context.TagName == "a")
            {
                output.Attributes.SetAttribute("href", "#");
            }
        }
    }
}
