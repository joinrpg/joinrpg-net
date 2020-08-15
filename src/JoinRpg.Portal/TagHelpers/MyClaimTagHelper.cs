using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JoinRpg.Portal.TagHelpers
{
    // You may need to install the Microsoft.AspNetCore.Razor.Runtime package into your project
    [HtmlTargetElement("my-claim-link")]
    public class MyClaimTagHelper : AnchorTagHelper
    {
        [HtmlAttributeName("asp-for")]
        public IProjectIdAware For { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.Remove(output.Attributes.Single(a => a.Name == "asp-for"));
            Controller = "Claim";
            Action = "MyClaim";
            RouteValues.Add("ProjectId", For.ProjectId.ToString());
            base.Process(context, output);

        }

        public MyClaimTagHelper(IHtmlGenerator generator) : base(generator)
        {
        }
    }
}
