using Markdig;
using Markdig.Renderers;

namespace JoinRpg.Markdown;

internal class EntityLinkerExtension : IMarkdownExtension
{
    private ILinkRenderer LinkRenderers { get; }

    public EntityLinkerExtension(ILinkRenderer linkRenderers) => LinkRenderers = linkRenderers ?? throw new ArgumentNullException(nameof(linkRenderers));

    public void Setup(MarkdownPipelineBuilder pipeline) => pipeline.InlineParsers.AddIfNotAlready(new LinkerParser(LinkRenderers));

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) => renderer.ObjectRenderers.AddIfNotAlready(new LinkerRenderAdapter(LinkRenderers));
}
