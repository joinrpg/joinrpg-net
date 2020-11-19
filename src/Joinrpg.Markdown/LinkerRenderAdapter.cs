using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace JoinRpg.Markdown
{
    internal class LinkerRenderAdapter : HtmlObjectRenderer<EntityLinkInline>
    {
        private ILinkRenderer LinkRenderer { get; }

        public LinkerRenderAdapter(ILinkRenderer linkRenderer) => LinkRenderer = linkRenderer;

        protected override void Write(HtmlRenderer renderer, EntityLinkInline obj) => renderer.Write(LinkRenderer.Render(obj.Match, obj.Index, obj.Extra));
    }
}
