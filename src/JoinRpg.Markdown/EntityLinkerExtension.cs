using Markdig;
using Markdig.Renderers;

namespace JoinRpg.Markdown;

internal class EntityLinkerExtension(string[] prefixes) : IMarkdownExtension
{
    private readonly string[] prefixes = prefixes;

    public void Setup(MarkdownPipelineBuilder pipeline) => pipeline.InlineParsers.AddIfNotAlready(new LinkerParser(prefixes));

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) => renderer.ObjectRenderers.AddIfNotAlready(new LinkerRenderAdapter());
}
