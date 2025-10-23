using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace JoinRpg.Markdown;

internal static class MarkdownEntityLinkerExtensions
{
    public static MarkdownPipelineBuilder UseEntityLinker(this MarkdownPipelineBuilder pipeline, string[] prefixes)
    {
        pipeline.Extensions.AddIfNotAlready(new EntityLinkerExtension(prefixes));
        return pipeline;
    }

    internal class EntityLinkerExtension(string[] prefixes) : IMarkdownExtension
    {
        private readonly string[] prefixes = prefixes;

        public void Setup(MarkdownPipelineBuilder pipeline) => pipeline.InlineParsers.AddIfNotAlready(new LinkerParser(prefixes));

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) => renderer.ObjectRenderers.AddIfNotAlready(new LinkerRenderAdapter());

        internal class LinkerRenderAdapter : HtmlObjectRenderer<EntityLinkInline>
        {
            protected override void Write(HtmlRenderer renderer, EntityLinkInline obj) => obj.Renderer.Render(renderer, obj.Match, obj.Index, obj.Extra);
        }
    }
}
