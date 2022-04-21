using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JoinRpg.Portal.TagHelpers;

[HtmlTargetElement("my-claim-link")]
public class MyClaimTagHelper : AnchorTagHelper
{
    [HtmlAttributeName("asp-for")]
    public IProjectIdAware For { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        Controller = "Claim";
        Action = "MyClaim";
        RouteValues.Add("ProjectId", For.ProjectId.ToString());
        base.Process(context, output);
        output.TagName = "a";
    }

    public MyClaimTagHelper(IHtmlGenerator generator) : base(generator)
    {
    }
}
