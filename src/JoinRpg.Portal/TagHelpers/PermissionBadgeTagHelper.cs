using System.Text.Encodings.Web;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
            _htmlHelper = (HtmlHelper)htmlHelper;
            _htmlEncoder = htmlEncoder;
        }

        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; } = null!;

        [ViewContext]
        public ViewContext ViewContext
        {
            set => _htmlHelper.Contextualize(value);
        }


        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;

            var partialView = await _htmlHelper.PartialAsync("PermissionBadge", new PermissionBadgeViewModel
            {
                Value = (bool)For.Model == true,
                Description = For.Metadata.Description,
                DisplayName = For.Metadata.DisplayName,
            });

            var writer = new StringWriter();
            partialView.WriteTo(writer, _htmlEncoder);

            _ = output.Content.SetHtmlContent(writer.ToString());
        }
    }
}
