using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace JoinRpg.Markdown;

internal class LinkerRenderAdapter : HtmlObjectRenderer<EntityLinkInline>
{
    protected override void Write(HtmlRenderer renderer, EntityLinkInline obj)
        => renderer.Write(obj.Renderer.Render(obj.Match, obj.Index, obj.Extra));
}
